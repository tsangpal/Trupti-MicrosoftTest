import * as Rx from '../../dist/cjs/Rx';
declare const {hot, cold, asDiagram, expectObservable, expectSubscriptions};

declare const rxTestScheduler: Rx.TestScheduler;
const Observable = Rx.Observable;

/** @test {timeoutWith} */
describe('Observable.prototype.timeoutWith', () => {
  asDiagram('timeoutWith(50)')('should timeout after a specified period then subscribe to the passed observable', () => {
    const e1 =  cold('-------a--b--|');
    const e1subs =   '^    !        ';
    const e2 =       cold('x-y-z-|  ');
    const e2subs =   '     ^     !  ';
    const expected = '-----x-y-z-|  ';

    const result = e1.timeoutWith(50, e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should timeout at a specified date then subscribe to the passed observable', () => {
    const e1 =  cold('-');
    const e1subs =   '^         !           ';
    const e2 = cold(           '--x--y--z--|');
    const e2subs =   '          ^          !';
    const expected = '------------x--y--z--|';

    const result = e1.timeoutWith(new Date(rxTestScheduler.now() + 100), e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should timeout after a specified period between emit then subscribe ' +
  'to the passed observable when source emits', () => {
    const e1 =     hot('---a---b------c---|');
    const e1subs =     '^          !       ';
    const e2 = cold(              '-x-y-|  ');
    const e2subs =     '           ^    !  ';
    const expected =   '---a---b----x-y-|  ';

    const result = e1.timeoutWith(40, e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should allow unsubscribing explicitly and early', () => {
    const e1 =     hot('---a---b-----c----|');
    const e1subs =     '^          !       ';
    const e2 = cold(              '-x---y| ');
    const e2subs =     '           ^  !    ';
    const expected =   '---a---b----x--    ';
    const unsub =      '              !    ';

    const result = e1.timeoutWith(40, e2, rxTestScheduler);

    expectObservable(result, unsub).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should not break unsubscription chain when unsubscribed explicitly', () => {
    const e1 =     hot('---a---b-----c----|');
    const e1subs =     '^          !       ';
    const e2 = cold(              '-x---y| ');
    const e2subs =     '           ^  !    ';
    const expected =   '---a---b----x--    ';
    const unsub =      '              !    ';

    const result = e1
      .mergeMap((x: string) => Observable.of(x))
      .timeoutWith(40, e2, rxTestScheduler)
      .mergeMap((x: string) => Observable.of(x));

    expectObservable(result, unsub).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should not subscribe to withObservable after explicit unsubscription', () => {
    const e1 =  cold('---a------b------');
    const e1subs =   '^    !           ';
    const e2 =  cold(        'i---j---|');
    const e2subs = [];
    const expected = '---a--           ';
    const unsub =    '     !           ';

    const result = e1
      .mergeMap((x: string) => Observable.of(x))
      .timeoutWith(50, e2, rxTestScheduler)
      .mergeMap((x: string) => Observable.of(x));

    expectObservable(result, unsub).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should timeout after a specified period then subscribe to the ' +
  'passed observable when source is empty', () => {
    const e1 =   hot('-------------|      ');
    const e1subs =   '^         !         ';
    const e2 = cold(           '----x----|');
    const e2subs =   '          ^        !';
    const expected = '--------------x----|';

    const result = e1.timeoutWith(100, e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should timeout after a specified period between emit then never completes ' +
  'if other source does not complete', () => {
    const e1 =   hot('--a--b--------c--d--|');
    const e1subs =   '^        !           ';
    const e2 =  cold('-');
    const e2subs =   '         ^           ';
    const expected = '--a--b----           ';

    const result = e1.timeoutWith(40, e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should timeout after a specified period then subscribe to the ' +
  'passed observable when source raises error after timeout', () => {
    const e1 =   hot('-------------#      ');
    const e1subs =   '^         !         ';
    const e2 =  cold(          '----x----|');
    const e2subs =   '          ^        !';
    const expected = '--------------x----|';

    const result = e1.timeoutWith(100, e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should timeout after a specified period between emit then never completes ' +
  'if other source emits but not complete', () => {
    const e1 =   hot('-------------|     ');
    const e1subs =   '^         !        ';
    const e2 =            cold('----x----');
    const e2subs =   '          ^        ';
    const expected = '--------------x----';

    const result = e1.timeoutWith(100, e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should not timeout if source completes within timeout period', () => {
    const e1 =   hot('-----|');
    const e1subs =   '^    !';
    const e2 = cold(           '----x----');
    const e2subs = [];
    const expected = '-----|';

    const result = e1.timeoutWith(100, e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should not timeout if source raises error within timeout period', () => {
    const e1 =   hot('-----#');
    const e1subs =   '^    !';
    const e2 = cold(           '----x----|');
    const e2subs = [];
    const expected = '-----#';

    const result = e1.timeoutWith(100, e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should not timeout if source emits within timeout period', () => {
    const e1 =   hot('--a--b--c--d--e--|');
    const e1subs =   '^                !';
    const e2 =  cold('----x----|');
    const e2subs = [];
    const expected = '--a--b--c--d--e--|';

    const result = e1.timeoutWith(50, e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should timeout after specified Date then subscribe to the passed observable', () => {
    const e1 =   hot('--a--b--c--d--e--|');
    const e1subs =   '^      !          ';
    const e2 =  cold(       '--z--|     ');
    const e2subs =   '       ^    !     ';
    const expected = '--a--b---z--|     ';

    const result = e1.timeoutWith(new Date(rxTestScheduler.now() + 70), e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should not timeout if source completes within specified Date', () => {
    const e1 =   hot('--a--b--c--d--e--|');
    const e1subs =   '^                !';
    const e2 =  cold('--x--|');
    const e2subs = [];
    const expected = '--a--b--c--d--e--|';

    const timeoutValue = new Date(Date.now() + (expected.length + 2) * 10);

    const result = e1.timeoutWith(timeoutValue, e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should not timeout if source raises error within specified Date', () => {
    const e1 =   hot('---a---#');
    const e1subs =   '^      !';
    const e2 =  cold('--x--|');
    const e2subs = [];
    const expected = '---a---#';

    const result = e1.timeoutWith(new Date(Date.now() + 100), e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });

  it('should timeout specified Date after specified Date then never completes ' +
  'if other source does not complete', () => {
    const e1 =   hot('---a---b---c---d---e---|');
    const e1subs =   '^         !             ';
    const e2 =  cold('-');
    const e2subs =   '          ^             ';
    const expected = '---a---b---             ';

    const result = e1.timeoutWith(new Date(rxTestScheduler.now() + 100), e2, rxTestScheduler);

    expectObservable(result).toBe(expected);
    expectSubscriptions(e1.subscriptions).toBe(e1subs);
    expectSubscriptions(e2.subscriptions).toBe(e2subs);
  });
});
