// Native Ollama and OpenAI-compatible xAI shapes from the provider API docs.
// Local verification only: require the test token and reject unmodeled paths.
const http = require('http');
http.createServer((req, res) => {
  if (req.headers.authorization !== 'Bearer cloud-test-key') {
    res.writeHead(401, { 'Content-Type': 'application/json' });
    return res.end(JSON.stringify({ error: 'Missing bearer authentication' }));
  }
  const path = req.url.split('?')[0];
  let chunks = '';
  req.on('data', data => { chunks += data; });
  req.on('end', () => {
    res.setHeader('Content-Type', 'application/json');
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
