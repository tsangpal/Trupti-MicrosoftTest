var React = require('react');
var loadJSON = require('./loadJSON');


// API endpoints
// https://registry.npmjs.org/immutable/latest
// https://api.github.com/repos/facebook/immutable-js

var StarBtn = React.createClass({

  getInitialState: function() {
    return { stars: null };
  },

  componentDidMount: function () {
    loadJSON('https://api.github.com/repos/facebook/immutable-js', value => {
      value && value.stargazers_count &&
        this.setState({stars: value.stargazers_count});
    });
  },

  render: function() {
    return (
      <span className="github-btn">
        <a className="gh-btn" id="gh-btn" href="https://github.com/facebook/immutable-js/">
          <span className="gh-ico"></span>
          <span className="gh-text">Star</span>
        </a>
        {this.state.stars &&
          <a className="gh-count" href="https://github.com/facebook/immutable-js/stargazers">
            {this.state.stars}
          </a>
        }
      </span>
    );
  }

});

module.exports = StarBtn;
