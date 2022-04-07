///<reference path='../resources/jest.d.ts'/>
///<reference path='../dist/immutable.d.ts'/>
jest.autoMockOff();

import * as jasmineCheck from 'jasmine-check';
jasmineCheck.install();

import { Seq } from 'immutable';

describe('IndexedSequence', () => {

  it('maintains skipped offset', () => {
    var seq = Seq(['A', 'B', 'C', 'D', 'E']);

    // This is what we expect for IndexedSequences
    var operated = seq.skip(1);
    expect(operated.entrySeq().toArray()).toEqual([
      [0, 'B'],
      [1, 'C'],
      [2, 'D'],
      [3, 'E']
    ]);

    expect(operated.first()).toEqual('B');
  });

  it('reverses correctly', () => {
    var seq = Seq(['A', 'B', 'C', 'D', 'E']);

    // This is what we expect for IndexedSequences
    var operated = seq.reverse();
    expect(operated.get(0)).toEqual('E');
    expect(operated.get(1)).toEqual('D');
    expect(operated.get(4)).toEqual('A');

    expect(operated.first()).toEqual('E');
    expect(operated.last()).toEqual('A');
  });

  it('negative indexes correctly', () => {
    var seq = Seq(['A', 'B', 'C', 'D', 'E']);

    expect(seq.first()).toEqual('A');
    expect(seq.last()).toEqual('E');
    expect(seq.get(-0)).toEqual('A');
    expect(seq.get(2)).toEqual('C');
    expect(seq.get(-2)).toEqual('D');

    var indexes = seq.keySeq();
    expect(indexes.first()).toEqual(0);
    expect(indexes.last()).toEqual(4);
    expect(indexes.get(-0)).toEqual(0);
    expect(indexes.get(2)).toEqual(2);
    expect(indexes.get(-2)).toEqual(3);
  });
});
