  var Observable = Rx.Observable,
    ObservableBase = Rx.ObservableBase,
    AbstractObserver = Rx.internals.AbstractObserver,
    CompositeDisposable = Rx.CompositeDisposable,
    BinaryDisposable = Rx.BinaryDisposable,
    RefCountDisposable = Rx.RefCountDisposable,
    SingleAssignmentDisposable = Rx.SingleAssignmentDisposable,
    SerialDisposable = Rx.SerialDisposable,
    Subject = Rx.Subject,
    observableProto = Observable.prototype,
    observableEmpty = Observable.empty,
    observableNever = Observable.never,
    AnonymousObservable = Rx.AnonymousObservable,
    addRef = Rx.internals.addRef,
    inherits = Rx.internals.inherits,
    bindCallback = Rx.internals.bindCallback,
    noop = Rx.helpers.noop,
    isPromise = Rx.helpers.isPromise,
    isFunction = Rx.helpers.isFunction,
    observableFromPromise = Observable.fromPromise;