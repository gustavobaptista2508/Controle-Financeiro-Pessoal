const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('granaok', {
  invoke: (action, payload = {}) => ipcRenderer.invoke('granaok:invoke', action, payload),
  platform: process.platform,
  version: '0.1.3'
});
