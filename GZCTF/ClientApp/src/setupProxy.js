const createProxyMiddleware = require('http-proxy-middleware');
const { env } = require('process');

const target = 'http://localhost:5000';

const httpEndpoints = ['/api', '/swagger'];
const wsEndpoints = ['/hub'];

module.exports = function (app) {
  const httpProxy = createProxyMiddleware(httpEndpoints, {
    target: target,
    secure: false,
    changeOrigin: true
  });

  const wsProxy = createProxyMiddleware(wsEndpoints, {
    target: target,
    secure: false,
    changeOrigin: true,
    ws: true
  });

  app.use(httpProxy);
  app.use(wsProxy);
};
