/**
 *  Copyright (c) 2014-2015, Facebook, Inc.
 *  All rights reserved.
 *
 *  This source code is licensed under the BSD-style license found in the
 *  LICENSE file in the root directory of this source tree. An additional grant
 *  of patent rights can be found in the PATENTS file in the same directory.
 */

import { KeyedIterable } from './Iterable'
import { KeyedCollection } from './Collection'
import { Map, MapPrototype, emptyMap } from './Map'
import { DELETE } from './TrieUtils'

import invariant from './utils/invariant'


export class Record extends KeyedCollection {

  constructor(defaultValues, name) {
    var hasInitialized;

    var RecordType = function Record(values) {
      if (values instanceof RecordType) {
        return values;
      }
      if (!(this instanceof RecordType)) {
        return new RecordType(values);
      }
      if (!hasInitialized) {
        hasInitialized = true;
        var keys = Object.keys(defaultValues);
        setProps(RecordTypePrototype, keys);
        RecordTypePrototype.size = keys.length;
        RecordTypePrototype._name = name;
        RecordTypePrototype._keys = keys;
        RecordTypePrototype._defaultValues = defaultValues;
      }
      this._map = Map(values);
    };

    var RecordTypePrototype = RecordType.prototype = Object.create(RecordPrototype);
    RecordTypePrototype.constructor = RecordType;

    return RecordType;
  }

  toString() {
    return this.__toString(recordName(this) + ' {', '}');
  }

  // @pragma Access

  has(k) {
    return this._defaultValues.hasOwnProperty(k);
  }

  get(k, notSetValue) {
    if (!this.has(k)) {
      return notSetValue;
    }
    var defaultVal = this._defaultValues[k];
    return this._map ? this._map.get(k, defaultVal) : defaultVal;
  }

  // @pragma Modification

  clear() {
    if (this.__ownerID) {
      this._map && this._map.clear();
      return this;
    }
    var RecordType = this.constructor;
    return RecordType._empty || (RecordType._empty = makeRecord(this, emptyMap()));
  }

  set(k, v) {
    if (!this.has(k)) {
      throw new Error('Cannot set unknown key "' + k + '" on ' + recordName(this));
    }
    if (this._map && !this._map.has(k)) {
      var defaultVal = this._defaultValues[k];
      if (v === defaultVal) {
        return this;
      }
    }
    var newMap = this._map && this._map.set(k, v);
    if (this.__ownerID || newMap === this._map) {
      return this;
    }
    return makeRecord(this, newMap);
  }

  remove(k) {
    if (!this.has(k)) {
      return this;
    }
    var newMap = this._map && this._map.remove(k);
    if (this.__ownerID || newMap === this._map) {
      return this;
    }
    return makeRecord(this, newMap);
  }

  wasAltered() {
    return this._map.wasAltered();
  }

  __iterator(type, reverse) {
    return KeyedIterable(this._defaultValues).map((_, k) => this.get(k)).__iterator(type, reverse);
  }

  __iterate(fn, reverse) {
    return KeyedIterable(this._defaultValues).map((_, k) => this.get(k)).__iterate(fn, reverse);
  }

  __ensureOwner(ownerID) {
    if (ownerID === this.__ownerID) {
      return this;
    }
    var newMap = this._map && this._map.__ensureOwner(ownerID);
    if (!ownerID) {
      this.__ownerID = ownerID;
      this._map = newMap;
      return this;
    }
    return makeRecord(this, newMap, ownerID);
  }
}

var RecordPrototype = Record.prototype;
RecordPrototype[DELETE] = RecordPrototype.remove;
RecordPrototype.deleteIn =
RecordPrototype.removeIn = MapPrototype.removeIn;
RecordPrototype.merge = MapPrototype.merge;
RecordPrototype.mergeWith = MapPrototype.mergeWith;
RecordPrototype.mergeIn = MapPrototype.mergeIn;
RecordPrototype.mergeDeep = MapPrototype.mergeDeep;
RecordPrototype.mergeDeepWith = MapPrototype.mergeDeepWith;
RecordPrototype.mergeDeepIn = MapPrototype.mergeDeepIn;
RecordPrototype.setIn = MapPrototype.setIn;
RecordPrototype.update = MapPrototype.update;
RecordPrototype.updateIn = MapPrototype.updateIn;
RecordPrototype.withMutations = MapPrototype.withMutations;
RecordPrototype.asMutable = MapPrototype.asMutable;
RecordPrototype.asImmutable = MapPrototype.asImmutable;


function makeRecord(likeRecord, map, ownerID) {
  var record = Object.create(Object.getPrototypeOf(likeRecord));
  record._map = map;
  record.__ownerID = ownerID;
  return record;
}

function recordName(record) {
  return record._name || record.constructor.name || 'Record';
}

function setProps(prototype, names) {
  try {
    names.forEach(setProp.bind(undefined, prototype));
  } catch (error) {
    // Object.defineProperty failed. Probably IE8.
  }
}

function setProp(prototype, name) {
  Object.defineProperty(prototype, name, {
    get: function() {
      return this.get(name);
    },
    set: function(value) {
      invariant(this.__ownerID, 'Cannot set on an immutable record.');
      this.set(name, value);
    }
  });
}
