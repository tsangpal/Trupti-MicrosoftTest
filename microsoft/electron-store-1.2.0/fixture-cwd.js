'use strict';
const assert = require('assert');
const path = require('path');
const electron = require('electron');
const Store = require('.');

// Prevent Electron from never exiting when an exception happens
process.on('uncaughtException', err => {
	console.error('Exception:', err);
	process.exit(1); // eslint-disable-line
});

console.log(electron.app.getPath('userData'));

const store = new Store({cwd: 'foo'});
console.log(store.path);

const store2 = new Store({cwd: path.join(__dirname, 'bar')});
console.log(store2.path);

electron.app.quit();
