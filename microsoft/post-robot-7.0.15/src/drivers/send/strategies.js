/* @flow */

import { isSameDomain, isSameTopWindow, isActuallySameDomain, getActualDomain, getDomain } from 'cross-domain-utils/src';

import { CONSTANTS } from '../../conf';

export let SEND_MESSAGE_STRATEGIES = {};




SEND_MESSAGE_STRATEGIES[CONSTANTS.SEND_STRATEGIES.POST_MESSAGE] = (win : any, serializedMessage : string, domain : string) => {

    if (__IE_POPUP_SUPPORT__) {
        try {
            require('../../compat').emulateIERestrictions(window, win);
        } catch (err) {
            return;
        }
    }

    let domains;

    if (Array.isArray(domain)) {
        domains = domain;
    } else if (domain) {
        domains = [ domain ];
    } else {
        domains = [ CONSTANTS.WILDCARD ];
    }

    domains = domains.map(dom => {

        if (dom.indexOf(CONSTANTS.MOCK_PROTOCOL) === 0) {

            if (window.location.protocol === CONSTANTS.FILE_PROTOCOL) {
                return CONSTANTS.WILDCARD;
            }

            if (!isActuallySameDomain(win)) {
                throw new Error(`Attempting to send messsage to mock domain ${dom}, but window is actually cross-domain`);
            }

            return getActualDomain(win);
        }

        if (dom.indexOf(CONSTANTS.FILE_PROTOCOL) === 0) {
            return CONSTANTS.WILDCARD;
        }

        return dom;
    });

    domains.forEach(dom => {
        return win.postMessage(serializedMessage, dom);
    });
};

if (__IE_POPUP_SUPPORT__) {

    let sendBridgeMessage = require('../../bridge').sendBridgeMessage;

    SEND_MESSAGE_STRATEGIES[CONSTANTS.SEND_STRATEGIES.BRIDGE] = (win : any, serializedMessage : string, domain : string) => {

        if (isSameDomain(win)) {
            throw new Error(`Post message through bridge disabled between same domain windows`);
        }

        if (isSameTopWindow(window, win) !== false) {
            throw new Error(`Can only use bridge to communicate between two different windows, not between frames`);
        }

        return sendBridgeMessage(win, serializedMessage, domain);
    };

    SEND_MESSAGE_STRATEGIES[CONSTANTS.SEND_STRATEGIES.GLOBAL] = (win : any, serializedMessage : string, domain : string) => {

        if (!isSameDomain(win)) {
            throw new Error(`Post message through global disabled between different domain windows`);
        }

        if (isSameTopWindow(window, win) !== false) {
            throw new Error(`Can only use global to communicate between two different windows, not between frames`);
        }

        let foreignGlobal = win[CONSTANTS.WINDOW_PROPS.POSTROBOT];

        if (!foreignGlobal) {
            throw new Error(`Can not find postRobot global on foreign window`);
        }

        return foreignGlobal.receiveMessage({
            source: window,
            origin: getDomain(),
            data: serializedMessage
        });
    };
}
