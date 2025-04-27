echo "Install webpack and set up package.json..."
npm install webpack webpack-cli --save-dev
sed -i '/"scripts": {/,/}/c\  \"scripts": {\n    \
"buildCodemirror": "webpack ./src/codemirror.js --output-path ../wwwroot/js --output-filename codemirror.bundle.js",\n    \
"buildSignalr": "webpack ./src/signalr.js --output-path ../wwwroot/js --output-filename signalr.bundle.js",\n     \
"buildCompile": "webpack ./src/compile.js --output-path ../wwwroot/js --output-filename compile.bundle.js"\n     \
 },' package.json
echo "Installing codemirror related node packages..."
npm install codemirror codemirror/theme-one-dark @uiw/codemirror-theme-duotone @babel/runtime codemirror/lang-javascript codemirror/lang-python codemirror/lang-css codemirror/lang-html codemirror/commands
echo "Installing Yjs related node packages..."
npm install yjs y-codemirror.next
echo "Building CodeMirror bundle..."
npm run buildCodemirror
echo "Installing SignalR related node packages..."
npm install @microsoft/signalr @microsoft/signalr-protocol-msgpack
echo "Building SignalR bundle..."
npm run buildSignalr
echo "Installing compile related node packages..."
npm install sprintf-js
echo "Building compile bundle..."
npm run buildCompile
echo "Done"
