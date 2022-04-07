/**
 * @author Toru Nagashima
 * @copyright 2016 Toru Nagashima. All rights reserved.
 * See LICENSE file in root directory for full license.
 */
"use strict"

/*eslint-disable no-var, prefer-arrow-callback*/

//------------------------------------------------------------------------------
// Requirements
//------------------------------------------------------------------------------

var fs = require("fs")
var Promise = require("pinkie-promise")

//------------------------------------------------------------------------------
// Helpers
//------------------------------------------------------------------------------

var FILE_NAME = "test.txt"

//------------------------------------------------------------------------------
// Public Interface
//------------------------------------------------------------------------------

/**
 * Gets the result text from `test.txt`.
 *
 * @returns {string|null} The result text.
 */
exports.result = function result() {
    try {
        return fs.readFileSync(FILE_NAME, {encoding: "utf8"})
    }
    catch (err) {
        if (err.message.indexOf("ENOENT") < 0) {
            console.error("ERROR:", err.stack)
        }
        return null
    }
}

/**
 * Appends text to `test.txt`.
 *
 * @param {string} content - A text to append.
 * @returns {void}
 */
exports.appendResult = function appendResult(content) {
    fs.appendFileSync(FILE_NAME, content)
}

/**
 * Removes `test.txt`.
 *
 * @returns {void}
 */
exports.removeResult = function removeResult() {
    try {
        fs.unlinkSync(FILE_NAME)
    }
    catch (err) {
        if (err.message.indexOf("ENOENT") < 0) {
            console.error("ERROR:", err.stack)
        }
    }
}

/**
 * Delay.
 *
 * @param {number} timeoutInMillis - The time to delay.
 * @returns {Promise<void>} The promise which fulfilled after the given time.
 */
exports.delay = function delay(timeoutInMillis) {
    return new Promise(function(resolve) {
        setTimeout(resolve, timeoutInMillis)
    })
}
