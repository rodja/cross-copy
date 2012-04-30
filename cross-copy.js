var port = 8124;
var host = "127.0.0.1";

var http = require('http');
http.createServer(function (req, res) {
  if (true) {
     res.writeHead(200, {'Content-Type': 'text/plain'});
     res.end('Hello World\n');
  }
}).listen(port, host);

console.log('cross-copy is running at http://' + host + ':' + port + '/');
