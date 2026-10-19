// Native Ollama and OpenAI-compatible xAI shapes from the provider API docs.
// Local verification only: require the test token and reject unmodeled paths.
const http = require('http');
http.createServer((req, res) => {
  if (!req.url.startsWith('/native/') && req.headers.authorization !== 'Bearer cloud-test-key') {
    res.writeHead(401, { 'Content-Type': 'application/json' });
    return res.end(JSON.stringify({ error: 'Missing bearer authentication' }));
  }
  const path = req.url.split('?')[0];
  let chunks = '';
  req.on('data', data => { chunks += data; });
  req.on('end', () => {
    res.setHeader('Content-Type', 'application/json');
    if (req.method === 'GET' && path === '/v1.0/endpoints/completion/cep_health/health') return res.end(JSON.stringify({ IsHealthy: true, LastError: null }));
    if (path.startsWith('/native/')) return native(req,res,path,chunks ? JSON.parse(chunks) : {});
    if (req.method === 'GET' && path === '/api/tags')
      return res.end(JSON.stringify({ models: [{ name: 'cloud-model', size: 0 }] }));
    if (req.method === 'GET' && path === '/v1/models')
      return res.end(JSON.stringify({ data: [{ id: 'cloud-model', owned_by: 'xai' }] }));
    if (req.method === 'POST' && ['/api/chat', '/v1/chat/completions'].includes(path)) {
      const body = JSON.parse(chunks);
      if (!body.model || !Array.isArray(body.messages)) {
        res.statusCode = 400;
        return res.end(JSON.stringify({ error: 'model and messages required' }));
      }
      if (body.stream) {
        if (path === '/api/chat') {
          res.setHeader('Content-Type', 'application/x-ndjson');
          return res.end(JSON.stringify({ message: { content: 'Cloud answer' }, done: false }) + '\n' + JSON.stringify({ done: true }) + '\n');
        }
        res.setHeader('Content-Type', 'text/event-stream');
        return res.end('data: ' + JSON.stringify({ choices: [{ delta: { content: 'Cloud answer' } }] }) + '\n\ndata: [DONE]\n\n');
      }
      return res.end(JSON.stringify(path === '/api/chat'
        ? { message: { role: 'assistant', content: 'Cloud answer' }, done: true }
        : { choices: [{ message: { role: 'assistant', content: 'Cloud answer' }, finish_reason: 'stop' }] }));
    }
    res.statusCode = 501;
    res.end(JSON.stringify({ error: 'Endpoint not modeled' }));
  });
}).listen(process.env.PORT);

// Responses follow the native parsers in PolyPrompt 3.4.0. Dummy credentials only.
function native(req, res, path, body) {
  const name = path.split('/')[2];
  const fail = message => { res.statusCode=400; res.end(JSON.stringify({error:message})); };
  if (name === 'anthropic' && (req.headers['x-api-key'] !== 'cloud-test-key' || !req.headers['anthropic-version'])) return fail('Anthropic headers missing');
  if (name === 'azure' && req.headers['api-key'] !== 'cloud-test-key') return fail('Azure api-key missing');
  if (name === 'bedrock' && !req.headers.authorization?.startsWith('AWS4-HMAC-SHA256')) return fail('AWS signing missing');
  if (['vertex','cohere'].includes(name) && req.headers.authorization !== 'Bearer cloud-test-key') return fail('Bearer missing');
  if (name === 'auth') {
    const mode = path.split('/')[3];
    const ok = mode === 'None' ? !req.headers.authorization : mode === 'Bearer' ? req.headers.authorization === 'Bearer cloud-test-key' : mode === 'Header' ? req.headers['x-endpoint-key'] === 'cloud-test-key' && !req.headers.authorization : mode === 'Basic' ? req.headers.authorization === 'Basic dXNlcjpjbG91ZC10ZXN0LWtleQ==' : req.url.includes('token=cloud-test-key') && !req.headers.authorization;
    if (!ok) return fail('Selectable authentication mismatch');
  }
  if (req.method === 'GET' && path.endsWith('/models')) return res.end(JSON.stringify({data:[{id:'cloud-model'}]}));
  if (name === 'failure') { res.statusCode=429; return res.end(JSON.stringify({error:{message:'Rate limited'}})); }
  const messages = body.messages || body.contents;
  if (!Array.isArray(messages) || messages.length < 2) return fail('Conversation history was lost');
  const hasTools = !!(body.tools || body.toolConfig);
  const tool = {id:'call_1',type:'function',function:{name:'collection_search',arguments:'{"query":"alpha"}'}};
  if (body.stream || req.url.includes(':streamGenerateContent')) {
    res.setHeader('Content-Type','text/event-stream');
    const event = (type,data) => `event: ${type}\ndata: ${JSON.stringify(data)}\n\n`;
    if (name === 'anthropic') return res.end(event('content_block_delta',{type:'content_block_delta',index:0,delta:{type:'text_delta',text:'Native answer'}})+event('message_delta',{type:'message_delta',delta:{stop_reason:'end_turn'},usage:{output_tokens:2}})+event('message_stop',{type:'message_stop'}));
    if (name === 'cohere') return res.end(event('content-delta',{type:'content-delta',delta:{message:{content:{text:'Native answer'}}}})+event('message-end',{type:'message-end',delta:{finish_reason:'COMPLETE',usage:{tokens:{input_tokens:4,output_tokens:2}}}}));
    if (name === 'vertex') return res.end('data: '+JSON.stringify({candidates:[{content:{role:'model',parts:[{text:'Native answer'}]},finishReason:'STOP'}]})+'\n\n');
    return res.end('data: '+JSON.stringify({choices:[{delta:{content:'Native answer'}}]})+'\n\ndata: [DONE]\n\n');
  }
  const text = {type:'text',text:'Native answer'};
  if (name === 'anthropic') return res.end(JSON.stringify({id:'msg_1',type:'message',role:'assistant',content:[text,...hasTools?[{type:'tool_use',id:'call_1',name:'collection_search',input:{query:'alpha'}}]:[]],stop_reason:hasTools?'tool_use':'end_turn',usage:{input_tokens:4,output_tokens:2}}));
  if (name === 'cohere') return res.end(JSON.stringify({id:'msg_1',message:{role:'assistant',content:[text],...hasTools?{tool_calls:[tool]}:{}},finish_reason:hasTools?'TOOL_CALL':'COMPLETE',usage:{tokens:{input_tokens:4,output_tokens:2}}}));
  if (name === 'bedrock') return res.end(JSON.stringify({output:{message:{role:'assistant',content:[{text:'Native answer'},...hasTools?[{toolUse:{toolUseId:'call_1',name:'collection_search',input:{query:'alpha'}}}]:[]]}},stopReason:hasTools?'tool_use':'end_turn',usage:{inputTokens:4,outputTokens:2,totalTokens:6}}));
  if (name === 'vertex') return res.end(JSON.stringify({candidates:[{content:{role:'model',parts:[{text:'Native answer'},...hasTools?[{functionCall:{name:'collection_search',args:{query:'alpha'}},thoughtSignature:'opaque-signature'}]:[]]},finishReason:'STOP'}],usageMetadata:{promptTokenCount:4,candidatesTokenCount:2,totalTokenCount:6}}));
  res.end(JSON.stringify({choices:[{message:{role:'assistant',content:'Native answer',...hasTools?{tool_calls:[tool]}:{}},finish_reason:hasTools?'tool_calls':'stop'}],usage:{prompt_tokens:4,completion_tokens:2,total_tokens:6}}));
}
