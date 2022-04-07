var React = require('react');
var assign = require('react/lib/Object.assign');
var Router = require('react-router');
var { Route, DefaultRoute, RouteHandler } = Router;
var DocHeader = require('./DocHeader');
var TypeDocumentation = require('./TypeDocumentation');
var defs = require('../../../lib/getTypeDefs');


var Documentation = React.createClass({
  render() {
    var typeDefURL = "https://github.com/facebook/immutable-js/blob/master/type-definitions/Immutable.d.ts";
    var issuesURL = "https://github.com/facebook/immutable-js/issues";
    return (
      <div>
        <DocHeader />
        <div className="pageBody" id="body">
          <div className="contents">
            <RouteHandler />
            <section className="disclaimer">
            This documentation is generated from <a href={typeDefURL}>Immutable.d.ts</a>.
            Pull requests and <a href={issuesURL}>Issues</a> welcome.
            </section>
          </div>
        </div>
      </div>
    );
  }
});

var DocDeterminer = React.createClass({
  mixins: [ Router.State ],

  render() {
    var { def, name, memberName } = determineDoc(this.getPath());
    return (
      <TypeDocumentation
        def={def}
        name={name}
        memberName={memberName}
      />
    );
  }
});


function determineDoc(path) {
  var [, name, memberName] = path.split('/');

  var namePath = name ? name.split('.') : [];
  var def = namePath.reduce(
    (def, subName) => def && def.module && def.module[subName],
    defs.Immutable
  );

  return { def, name, memberName };
}


module.exports = React.createClass({

  childContextTypes: {
    getPageData: React.PropTypes.func.isRequired,
  },

  getChildContext() {
    return {
      getPageData: this.getPageData,
    };
  },

  getPageData() {
    return this.pageData;
  },

  componentWillMount() {
    var location, scrollBehavior;

    if (window.document) {
      location = Router.HashLocation;
      location.addChangeListener(change => {
        this.pageData = assign({}, change, determineDoc(change.path));
      });

      this.pageData = !window.document ? {} : assign({
        path: location.getCurrentPath(),
        type: 'init',
      }, determineDoc(location.getCurrentPath()));

      scrollBehavior = {
        updateScrollPosition: (position, actionType) => {
          switch (actionType) {
            case 'push':
              return this.getPageData().memberName ?
                null :
                window.scrollTo(0, 0);
            case 'pop':
              return window.scrollTo(
                position ? position.x : 0,
                position ? position.y : 0
              );
          }
        }
      };
    }

    Router.create({
      routes:
        <Route handler={Documentation} path="/">
          <DefaultRoute handler={DocDeterminer} />
          <Route name="type" path="/:name" handler={DocDeterminer} />
          <Route name="method" path="/:name/:memberName" handler={DocDeterminer} />
        </Route>,
      location: location,
      scrollBehavior: scrollBehavior
    }).run(Handler => {
      this.setState({handler: Handler});
    });
  },

  // TODO: replace this. this is hacky and probably wrong

  componentDidMount() {
    setTimeout(() => { this.pageData.type = ''; }, 0);
  },

  componentDidUpdate() {
    setTimeout(() => { this.pageData.type = ''; }, 0);
  },

  render () {
    var Handler = this.state.handler;
    return <Handler />;
  }
});
