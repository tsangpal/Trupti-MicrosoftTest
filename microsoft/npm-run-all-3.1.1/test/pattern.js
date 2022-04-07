/**
 * @author Toru Nagashima
 * @copyright 2016 Toru Nagashima. All rights reserved.
 * See LICENSE file in root directory for full license.
 */
"use strict"

//------------------------------------------------------------------------------
// Requirements
//------------------------------------------------------------------------------

const assert = require("power-assert")
const {result, removeResult} = require("./lib/util")

// Test targets.
const nodeApi = require("../src/lib")
const runAll = require("../src/bin/npm-run-all")
const runSeq = require("../src/bin/run-s")
const runPar = require("../src/bin/run-p")

//------------------------------------------------------------------------------
// Test
//------------------------------------------------------------------------------

describe("[pattern] it should run matched tasks if glob like patterns are given.", () => {
    before(() => process.chdir("test-workspace"))
    after(() => process.chdir(".."))
    beforeEach(removeResult)

    describe("\"test-task:append:*\" to \"test-task:append:a\" and \"test-task:append:b\"", () => {
        it("Node API", () =>
            nodeApi("test-task:append:*")
                .then(() => {
                    assert(result() === "aabb")
                })
        )

        it("npm-run-all command", () =>
            runAll(["test-task:append:*"])
                .then(() => {
                    assert(result() === "aabb")
                })
        )

        it("run-s command", () =>
            runSeq(["test-task:append:*"])
                .then(() => {
                    assert(result() === "aabb")
                })
        )

        it("run-p command", () =>
            runPar(["test-task:append:*"])
                .then(() => {
                    assert(
                        result() === "abab" ||
                        result() === "abba" ||
                        result() === "baba" ||
                        result() === "baab"
                    )
                })
        )
    })

    describe("\"test-task:append:**:*\" to \"test-task:append:a\", \"test-task:append:a:c\", \"test-task:append:a:d\", and \"test-task:append:b\"", () => {
        it("Node API", () =>
            nodeApi("test-task:append:**:*")
                .then(() => {
                    assert(result() === "aaacacadadbb")
                })
        )

        it("npm-run-all command", () =>
            runAll(["test-task:append:**:*"])
                .then(() => {
                    assert(result() === "aaacacadadbb")
                })
        )

        it("run-s command", () =>
            runSeq(["test-task:append:**:*"])
                .then(() => {
                    assert(result() === "aaacacadadbb")
                })
        )
    })

    describe("(should ignore duplications) \"test-task:append:b\" \"test-task:append:*\" to \"test-task:append:b\", \"test-task:append:a\"", () => {
        it("Node API", () =>
            nodeApi(["test-task:append:b", "test-task:append:*"])
                .then(() => {
                    assert(result() === "bbaa")
                })
        )

        it("npm-run-all command", () =>
            runAll(["test-task:append:b", "test-task:append:*"])
                .then(() => {
                    assert(result() === "bbaa")
                })
        )

        it("run-s command", () =>
            runSeq(["test-task:append:b", "test-task:append:*"])
                .then(() => {
                    assert(result() === "bbaa")
                })
        )

        it("run-p command", () =>
            runPar(["test-task:append:b", "test-task:append:*"])
                .then(() => {
                    assert(
                        result() === "baba" ||
                        result() === "baab" ||
                        result() === "abab" ||
                        result() === "abba"
                    )
                })
        )
    })

    describe("\"a\" should not match to \"test-task:append:a\"", () => {
        it("Node API", () =>
            nodeApi("a")
                .then(
                    () => assert(false, "should not match"),
                    (err) => assert((/not found/i).test(err.message))
                )
        )

        it("npm-run-all command", () =>
            runAll(["a"])
                .then(
                    () => assert(false, "should not match"),
                    (err) => assert((/not found/i).test(err.message))
                )
        )

        it("run-s command", () =>
            runSeq(["a"])
                .then(
                    () => assert(false, "should not match"),
                    (err) => assert((/not found/i).test(err.message))
                )
        )

        it("run-p command", () =>
            runPar(["a"])
                .then(
                    () => assert(false, "should not match"),
                    (err) => assert((/not found/i).test(err.message))
                )
        )
    })
})
