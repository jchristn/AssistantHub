import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  build: {
    rolldownOptions: {
      output: {
        // Third-party libraries go in their own chunks: they change less often than the app (so browsers keep them
        // cached across releases) and no single chunk passes Vite's 500 kB warning.
        codeSplitting: {
          groups: [
            { name: 'vendor-react', test: /node_modules[\/](react|react-dom|react-router|react-router-dom|scheduler)[\/]/, priority: 30 },
            { name: 'vendor-highlight', test: /node_modules[\/](react-syntax-highlighter|refractor|prismjs|highlight\.js|lowlight)[\/]/, priority: 20 },
            { name: 'vendor-markdown', test: /node_modules[\/](react-markdown|remark-[^\/]+|rehype-[^\/]+|mdast-[^\/]+|micromark[^\/]*|unified|hast-[^\/]+|unist-[^\/]+|vfile[^\/]*)[\/]/, priority: 10 },
            { name: 'vendor', test: /node_modules[\/]/, priority: 0 },
          ],
        },
      },
    },
  },
  server: {
    port: 8501,
    proxy: {
      '/openapi.json': {
        target: 'http://localhost:8800',
        changeOrigin: true
      },
      '/v1.0': {
        target: 'http://localhost:8800',
        changeOrigin: true
      }
    }
  }
});
