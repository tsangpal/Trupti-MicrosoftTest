import React from 'react';
import scroll from 'scroll';
import autobind from 'react-autobind';
import nested from 'nested-property';
import { getRootEl, logger, sanitizeSelector } from './utils';

import Beacon from './Beacon';
import Tooltip from './Tooltip';

const defaultState = {
  action: '',
  index: 0,
  isRunning: false,
  isTourSkipped: false,
  shouldRedraw: true,
  shouldRenderTooltip: false,
  shouldRun: false,
  standaloneData: false, // The standalone tooltip data
  xPos: -1000,
  yPos: -1000
};

const callbackTypes = {
  STEP_BEFORE: 'step:before',
  BEACON_BEFORE: 'beacon:before',
  BEACON_TRIGGER: 'beacon:trigger',
  TOOLTIP_BEFORE: 'tooltip:before',
  STEP_AFTER: 'step:after',
  STANDALONE_BEFORE: 'standalone:before',
  STANDALONE_AFTER: 'standalone:after',
  OVERLAY: 'overlay:click',
  HOLE: 'hole:click',
  FINISHED: 'finished'
};

const listeners = {
  tooltips: {}
};

const STEP_DEFAULTS = {
  position: 'top',
};

let isTouch = false;
if (typeof window !== 'undefined') {
  isTouch = 'ontouchstart' in window || navigator.msMaxTouchPoints;
}

class Joyride extends React.Component {
  constructor(props) {
    super(props);
    autobind(this);

    this.state = defaultState;
  }

  static propTypes = {
    autoStart: React.PropTypes.bool,
    callback: React.PropTypes.func,
    debug: React.PropTypes.bool,
    disableOverlay: React.PropTypes.bool,
    holePadding: React.PropTypes.number,
    keyboardNavigation: React.PropTypes.bool,
    locale: React.PropTypes.object,
    resizeDebounce: React.PropTypes.bool,
    resizeDebounceDelay: React.PropTypes.number,
    run: React.PropTypes.bool,
    scrollOffset: React.PropTypes.number,
    scrollToFirstStep: React.PropTypes.bool,
    scrollToSteps: React.PropTypes.bool,
    showBackButton: React.PropTypes.bool,
    showOverlay: React.PropTypes.bool,
    showSkipButton: React.PropTypes.bool,
    showStepsProgress: React.PropTypes.bool,
    stepIndex: React.PropTypes.number,
    steps: React.PropTypes.array,
    tooltipOffset: React.PropTypes.number,
    type: React.PropTypes.string
  };

  static defaultProps = {
    autoStart: false,
    debug: false,
    holePadding: 5,
    keyboardNavigation: true,
    locale: {
      back: 'Back',
      close: 'Close',
      last: 'Last',
      next: 'Next',
      skip: 'Skip'
    },
    resizeDebounce: false,
    resizeDebounceDelay: 200,
    run: false,
    scrollToSteps: true,
    scrollOffset: 20,
    scrollToFirstStep: false,
    showBackButton: true,
    showOverlay: true,
    showSkipButton: false,
    showStepsProgress: false,
    steps: [],
    tooltipOffset: 15,
    type: 'single'
  };

  componentDidMount() {
    const {
      autoStart,
      keyboardNavigation,
      resizeDebounce,
      resizeDebounceDelay,
      run,
      steps,
      type
    } = this.props;

    logger({
      type: 'joyride:initialized',
      msg: [this.props],
      debug: this.props.debug,
    });

    const stepsAreValid = this.checkStepsValidity(steps);
    if (steps && stepsAreValid && run) {
      this.start(autoStart);
    }

    if (resizeDebounce) {
      let timeoutId;

      listeners.resize = () => {
        clearTimeout(timeoutId);
        timeoutId = setTimeout(() => {
          timeoutId = null;
          this.calcPlacement();
        }, resizeDebounceDelay);
      };
    }
    else {
      listeners.resize = () => {
        this.calcPlacement();
      };
    }
    window.addEventListener('resize', listeners.resize);

    if (keyboardNavigation && type === 'continuous') {
      listeners.keyboard = this.onKeyboardNavigation;
      document.body.addEventListener('keydown', listeners.keyboard);
    }
  }

  componentWillReceiveProps(nextProps) {
    logger({
      type: 'joyride:willReceiveProps',
      msg: [nextProps],
      debug: nextProps.debug,
    });
    const { isRunning, shouldRun, standaloneData } = this.state;
    const { keyboardNavigation, run, steps, stepIndex } = this.props;
    const stepsChanged = (nextProps.steps !== steps);
    const stepIndexChanged = (nextProps.stepIndex !== stepIndex && nextProps.stepIndex !== this.state.index);
    const runChanged = (nextProps.run !== run);
    let shouldStart = false;

    if (stepsChanged && this.checkStepsValidity(nextProps.steps)) {
      // Removed all steps, so reset
      if (!nextProps.steps || !nextProps.steps.length) {
        this.reset();
      }
      // Start the joyride if steps were added for the first time, and run prop is true
      else if (!steps.length && nextProps.run) {
        shouldStart = true;
      }
    }

    if (runChanged) {
      // run prop was changed to off, so stop the joyride
      if (run && nextProps.run === false) {
        this.stop();
      }
      // run prop was changed to on, so start the joyride
      else if (!run && nextProps.run) {
        shouldStart = true;
      }
      // Was not playing, but should, and isn't a standaloneData
      else if (!isRunning && (shouldRun && !standaloneData)) {
        shouldStart = true;
      }
    }

    if (stepIndexChanged) {
      const hasStep = nextProps.steps[nextProps.stepIndex];
      const shouldDisplay = hasStep && nextProps.autoStart;
      if (runChanged && shouldStart) {
        this.start(nextProps.autoStart, nextProps.steps, nextProps.stepIndex);
      }
      else {
        this.toggleTooltip({ show: shouldDisplay, index: nextProps.stepIndex, steps: nextProps.steps, action: 'jump' });
      }
    }

    // Did not change the index, but need to start up the joyride
    else if (shouldStart) {
      this.start(nextProps.autoStart, nextProps.steps);
    }

    // Update keyboard listeners if necessary
    if (
      !listeners.keyboard &&
      ((!keyboardNavigation && nextProps.keyboardNavigation) || keyboardNavigation)
      && nextProps.type === 'continuous'
    ) {
      listeners.keyboard = this.onKeyboardNavigation;
      document.body.addEventListener('keydown', listeners.keyboard);
    }
    else if (
      listeners.keyboard && keyboardNavigation &&
      (!nextProps.keyboardNavigation || nextProps.type !== 'continuous')
    ) {
      document.body.removeEventListener('keydown', listeners.keyboard);
      delete listeners.keyboard;
    }
  }

  componentWillUpdate(nextProps, nextState) {
    const { action, index, isRunning, shouldRenderTooltip, standaloneData } = this.state;
    const { steps } = this.props;
    const { steps: nextSteps } = nextProps;
    const step = steps[index];
    const nextStep = nextSteps[nextState.index];

    if (!standaloneData && nextState.standaloneData) {
      this.triggerCallback({
        type: callbackTypes.STANDALONE_BEFORE,
        step: nextState.standaloneData
      });
    }
    else if (standaloneData && !nextState.standaloneData) {
      this.triggerCallback({
        type: callbackTypes.STANDALONE_AFTER,
        step: standaloneData
      });
    }

    if ((!isRunning && nextState.isRunning) && index === 0) {
      this.triggerCallback({
        action: 'start',
        index,
        type: callbackTypes.STEP_BEFORE,
        step: nextStep
      });

      if (!nextState.shouldRenderTooltip) {
        this.triggerCallback({
          action: 'start',
          index: nextState.index,
          type: callbackTypes.BEACON_BEFORE,
          step: nextStep
        });
      }
    }

    if (nextState.index !== index && isRunning) {
      this.triggerCallback({
        action,
        index,
        type: callbackTypes.STEP_AFTER,
        step
      });

      if (nextState.index && nextStep) {
        this.triggerCallback({
          action: nextState.action,
          index: nextState.index,
          type: callbackTypes.STEP_BEFORE,
          step: nextStep
        });
      }
    }

    if (nextState.isRunning && (shouldRenderTooltip !== nextState.shouldRenderTooltip || nextState.index !== index)) {
      if (nextState.shouldRenderTooltip) {
        this.triggerCallback({
          action: nextState.action || (nextState.index === 0 ? 'autostart' : ''),
          index: nextState.index,
          type: callbackTypes.TOOLTIP_BEFORE,
          step: nextStep
        });
      }
      else {
        this.triggerCallback({
          action: nextState.action,
          index: nextState.index,
          type: callbackTypes.BEACON_BEFORE,
          step: nextStep
        });
      }
    }

    if (nextProps.run && nextSteps.length && !nextStep) {
      this.triggerCallback({
        action: nextState.action,
        type: callbackTypes.FINISHED,
        steps: nextSteps,
        isTourSkipped: nextState.isTourSkipped
      });
    }
  }

  componentDidUpdate(prevProps, prevState) {
    const { index, shouldRedraw, isRunning, shouldRun, standaloneData } = this.state;
    const { scrollToFirstStep, scrollToSteps, steps } = this.props;
    const shouldScroll = scrollToFirstStep || (index > 0 || prevState.index > index);

    if (shouldRedraw && steps[index]) {
      this.calcPlacement();
    }

    if (isRunning && scrollToSteps && shouldScroll) {
      scroll.top(getRootEl(), this.getScrollTop());
    }

    if (steps.length && (!isRunning && shouldRun && !standaloneData)) {
      this.start();
    }
  }

  componentWillUnmount() {
    window.removeEventListener('resize', listeners.resize);

    if (listeners.keyboard) {
      document.body.removeEventListener('keydown', listeners.keyboard);
    }

    if (Object.keys(listeners.tooltips).length) {
      Object.keys(listeners.tooltips).forEach((key) => {
        document.querySelector(key)
          .removeEventListener(listeners.tooltips[key].event, listeners.tooltips[key].cb);
        delete listeners.tooltips[key];
      });
    }
  }

  /**
   * Starts the tour
   *
   * @param {boolean} [autorun] - Starts with the first tooltip opened
   * @param {Array} [steps] - Array of steps, defaults to this.props.steps
   * @param {number} [startIndex] - Optional step index to start joyride at
   */
  start(autorun, steps = this.props.steps, startIndex) {
    const shouldRenderTooltip = autorun === true;

    logger({
      type: 'joyride:start',
      msg: ['autorun:', shouldRenderTooltip],
      debug: this.props.debug,
    });

    this.setState({
      index: typeof startIndex !== 'undefined' ? startIndex : this.state.index,
      isRunning: !!steps.length,
      shouldRenderTooltip,
      shouldRun: !steps.length
    });
  }

  /**
   * Stop the tour
   */
  stop() {
    logger({
      type: 'joyride:stop',
      debug: this.props.debug,
    });
    this.setState({
      isRunning: false,
      shouldRenderTooltip: false
    });
  }

  /**
   * Move to the next step, if there is one.  If there is no next step, hide the tooltip.
   */
  next() {
    const { index, shouldRenderTooltip } = this.state;
    const { steps } = this.props;
    const nextIndex = index + 1;

    const shouldDisplay = Boolean(steps[nextIndex]) && shouldRenderTooltip;

    logger({
      type: 'joyride:next',
      msg: ['new index:', nextIndex],
      debug: this.props.debug,
    });
    this.toggleTooltip({ show: shouldDisplay, index: nextIndex, action: 'next' });
  }

  /**
   * Move to the previous step, if there is one.  If there is no previous step, hide the tooltip.
   */
  back() {
    const { index, shouldRenderTooltip } = this.state;
    const { steps } = this.props;
    const previousIndex = index - 1;

    const shouldDisplay = Boolean(steps[previousIndex]) && shouldRenderTooltip;

    logger({
      type: 'joyride:back',
      msg: ['new index:', previousIndex],
      debug: this.props.debug,
    });
    this.toggleTooltip({ show: shouldDisplay, index: previousIndex, action: 'next' });
  }

  /**
   * Reset Tour
   *
   * @param {boolean} [restart] - Starts the new tour right away
   */
  reset(restart) {
    const { index, isRunning } = this.state;
    const shouldRestart = restart === true;

    const newState = JSON.parse(JSON.stringify(defaultState));
    newState.isRunning = shouldRestart;

    logger({
      type: 'joyride:reset',
      msg: ['restart:', shouldRestart],
      debug: this.props.debug,
    });
    // Force a re-render if necessary
    if (shouldRestart && isRunning === shouldRestart && index === 0) {
      this.forceUpdate();
    }

    this.setState(newState);
  }

  /**
   * Retrieve the current progress of your tour
   *
   * @returns {{index: number, percentageComplete: number, step: (object|null)}}
   */
  getProgress() {
    const { index } = this.state;
    const { steps } = this.props;

    logger({
      type: 'joyride:getProgress',
      msg: ['steps:', steps],
      debug: this.props.debug,
    });

    return {
      index,
      percentageComplete: parseFloat(((index / steps.length) * 100).toFixed(2).replace('.00', '')),
      step: steps[index]
    };
  }

  /**
   * Add standalone tooltip events
   *
   * @param {Object} data - Similar shape to a 'step', but for a single tooltip
   */
  addTooltip(data) {
    if (!this.checkStepValidity(data)) {
      return;
    }

    logger({
      type: 'joyride:addTooltip',
      msg: ['data:', data],
      debug: this.props.debug,
    });

    const key = data.trigger || sanitizeSelector(data.selector);
    const el = document.querySelector(key);
    if (!el) {
      return;
    }
    el.setAttribute('data-tooltip', JSON.stringify(data));

    const eventType = data.event || 'click';
    if (eventType === 'hover' && !isTouch) {
      listeners.tooltips[key] = { event: 'mouseenter', cb: this.onClickStandaloneTrigger };
      listeners.tooltips[`${key}mouseleave`] = { event: 'mouseleave', cb: this.onClickStandaloneTrigger };
      listeners.tooltips[`${key}click`] = {
        event: 'click',
        cb: (e) => {
          e.preventDefault();
        }
      };

      el.addEventListener('mouseenter', listeners.tooltips[key].cb);
      el.addEventListener('mouseleave', listeners.tooltips[`${key}mouseleave`].cb);
      el.addEventListener('click', listeners.tooltips[`${key}click`].cb);
    }
    else {
      listeners.tooltips[key] = { event: 'click', cb: this.onClickStandaloneTrigger };
      el.addEventListener('click', listeners.tooltips[key].cb);
    }
  }

  /**
   * Parse the incoming steps
   *
   * @deprecated
   *
   * @param {Array|Object} steps
   * @returns {Array}
   */
  parseSteps(steps) {
    logger({
      type: 'joyride:parseSteps',
      msg: 'joyride.parseSteps() is deprecated.  It is no longer necessary to parse steps before providing them to Joyride.',
      warn: true,
      debug: this.props.debug,
    });
    return steps;
  }

  /**
   * Verify that a step is valid
   *
   * @param {Object} step - A step object
   * @returns {boolean} - True if the step is valid, false otherwise
   */
  checkStepValidity(step) {
    // Check that the step is the proper type
    if (!step || typeof step !== 'object' || Array.isArray(step)) {
      logger({
        type: 'joyride:checkStepValidity',
        msg: 'Did not provide a step object.',
        warn: true,
        debug: this.props.debug,
      });
      return false;
    }

    // Check that all required step fields are present
    const requiredFields = ['selector', 'text'];
    const hasRequiredField = (requiredField) => {
      const hasField = Boolean(step[requiredField]);
      if (!hasField) {
        logger({
          type: 'joyride:checkStepValidity',
          msg: [`Provided a step without the required ${requiredField} property.`, 'Step:', step],
          warn: true,
          debug: this.props.debug,
        });
      }
      return hasField;
    };
    return requiredFields.every(hasRequiredField);
  }

  /**
   * Check one or more steps are valid
   *
   * @param {Object|Array} steps - A step object or array of step objects
   * @returns {boolean} - True if one or more stpes, and all steps are valid, false otherwise
   */
  checkStepsValidity(steps) {
    if (!Array.isArray(steps) && typeof steps === 'object') {
      return this.checkStepValidity(steps);
    }
    else if (steps.length > 0) {
      return steps.every(this.checkStepValidity);
    }
    return false;
  }

  /**
   * Find and return the targeted DOM element based on a step's 'selector'.
   *
   * @private
   * @param {Object} step - A step object
   * @returns {Element} - A DOM element (if found)
   */
  getStepTargetElement(step) {
    const isValidStep = this.checkStepValidity(step);
    if (!isValidStep) {
      return null;
    }

    const el = document.querySelector(sanitizeSelector(step.selector));
    if (!el) {
      logger({
        type: 'joyride:getStepTargetElement',
        msg: 'Target not rendered. For best results only add steps after they are mounted.',
        warn: true,
        debug: this.props.debug,
      });
      return null;
    }
    return el;
  }

  /**
   * Get an element actual dimensions with margin
   *
   * @private
   * @param {String|Element} el - Element node or selector
   * @returns {{height: number, width: number}}
   */
  getElementDimensions(el) {
    // Get the DOM Node if you pass in a string
    const newEl = (typeof el === 'string') ? document.querySelector(el) : el;
    let height = 0;
    let width = 0;

    if (newEl) {
      const styles = window.getComputedStyle(newEl);
      height = newEl.clientHeight + parseInt(styles.marginTop, 10) + parseInt(styles.marginBottom, 10);
      width = newEl.clientWidth + parseInt(styles.marginLeft, 10) + parseInt(styles.marginRight, 10);
    }

    return {
      height,
      width
    };
  }

  /**
   * Get the scrollTop position
   *
   * @private
   * @returns {number}
   */
  getScrollTop() {
    const { index, yPos } = this.state;
    const { scrollOffset, steps } = this.props;
    const step = steps[index];
    const target = this.getStepTargetElement(step);

    if (!target) {
      return 0;
    }

    const rect = target.getBoundingClientRect();
    const targetTop = rect.top + (window.pageYOffset || document.documentElement.scrollTop);
    const position = this.calcPosition(step);
    let scrollTo = 0;

    if (/^top/.test(position)) {
      scrollTo = Math.floor(yPos - scrollOffset);
    }
    else if (/^bottom|^left|^right/.test(position)) {
      scrollTo = Math.floor(targetTop - scrollOffset);
    }

    return scrollTo;
  }

  /**
   * Trigger the callback.
   *
   * @private
   * @param {Object} options
   */
  triggerCallback(options) {
    const { callback } = this.props;

    if (typeof callback === 'function') {
      logger({
        type: 'joyride:triggerCallback',
        msg: [options],
        debug: this.props.debug,
      });

      callback(options);
    }
  }

  /**
   * Keydown event listener
   *
   * @private
   * @param {Event} e - Keyboard event
   */
  onKeyboardNavigation(e) {
    const { index, shouldRenderTooltip } = this.state;
    const { steps } = this.props;
    const intKey = (window.Event) ? e.which : e.keyCode;
    let hasSteps;

    if (shouldRenderTooltip) {
      if ([32, 38, 40].indexOf(intKey) > -1) {
        e.preventDefault();
      }

      if (intKey === 27) {
        this.toggleTooltip({ show: false, index: index + 1, action: 'esc' });
      }
      else if ([13, 32].indexOf(intKey) > -1) {
        hasSteps = Boolean(steps[index + 1]);
        this.toggleTooltip({ show: hasSteps, index: index + 1, action: 'next' });
      }
    }
  }

  /**
   * Tooltip event listener
   *
   * @private
   * @param {Event} e - Click event
   */
  onClickStandaloneTrigger(e) {
    e.preventDefault();
    const { isRunning, standaloneData } = this.state;
    let tooltipData = e.currentTarget.dataset.tooltip;

    if (tooltipData) {
      tooltipData = JSON.parse(tooltipData);

      if (!standaloneData || (standaloneData.selector !== tooltipData.selector)) {
        this.setState({
          isRunning: false,
          position: undefined,
          shouldRenderTooltip: false,
          shouldRun: isRunning,
          standaloneData: tooltipData,
          xPos: -1000,
          yPos: -1000
        });
      }
      else {
        document.querySelector('.joyride-tooltip__close').click();
      }
    }
  }

  /**
   * Beacon click event listener
   *
   * @private
   * @param {Event} e - Click event
   */
  onClickBeacon(e) {
    e.preventDefault();
    const { index } = this.state;
    const { steps } = this.props;

    this.triggerCallback({
      action: e.type,
      index,
      type: callbackTypes.BEACON_TRIGGER,
      step: steps[index]
    });

    this.toggleTooltip({ show: true, index, action: `beacon:${e.type}` });
  }

  /**
   * Tooltip click event listener
   *
   * @private
   * @param {Event} e - Click event
   */
  onClickTooltip(e) {
    const { index, shouldRun } = this.state;
    const { steps, type } = this.props;
    const el = e.currentTarget.className.indexOf('joyride-') === 0 && e.currentTarget.tagName === 'A' ? e.currentTarget : e.target;
    const dataType = el.dataset.type;

    if (el.className.indexOf('joyride-') === 0) {
      e.preventDefault();
      e.stopPropagation();
      const tooltip = document.querySelector('.joyride-tooltip');
      let newIndex = index + (dataType === 'back' ? -1 : 1);

      if (dataType === 'skip') {
        this.setState({
          isTourSkipped: true
        });
        newIndex = steps.length + 1;
      }

      if (tooltip.classList.contains('joyride-tooltip--standalone')) {
        this.setState({
          isRunning: shouldRun,
          shouldRedraw: true,
          shouldRun: false,
          standaloneData: false
        });
      }
      else if (dataType) {
        const shouldDisplay = ['continuous', 'guided'].indexOf(type) > -1
          && ['close', 'skip'].indexOf(dataType) === -1
          && Boolean(steps[newIndex]);

        this.toggleTooltip({ show: shouldDisplay, index: newIndex, action: dataType });
      }

      if (e.target.className === 'joyride-overlay') {
        this.triggerCallback({
          action: 'click',
          type: callbackTypes.OVERLAY,
          step: steps[index]
        });
      }

      if (e.target.classList.contains('joyride-hole')) {
        this.triggerCallback({
          action: 'click',
          type: callbackTypes.HOLE,
          step: steps[index]
        });
      }
    }
  }

  onRenderTooltip() {
    this.calcPlacement();
  }

  /**
   * Toggle Tooltip's visibility
   *
   * @private
   * @param {Object} options - Immediately destructured argument object
   * @param {Boolean} options.show - Render the tooltip or the beacon, defaults to opposite of current show
   * @param {Number} options.index - The tour's new index, defaults to current index
   * @param {string} [options.action] - The action being undertaken.
   * @param {Array} [options.steps] - The array of step objects that is going to be rendered
   */
  toggleTooltip({ show, index, action, steps = this.props.steps }) {
    let nextIndex = index;
    const nextStep = steps[nextIndex];

    if (nextStep && !this.getStepTargetElement(nextStep)) {
      console.warn('Target not mounted, skipping...', nextStep, action); //eslint-disable-line no-console
      nextIndex += action === 'back' ? -1 : 1;
    }

    this.setState({
      action,
      index: nextIndex,
      isRunning: nextStep ? this.state.isRunning : false, // stop playing if there is no next step
      shouldRedraw: !show,
      shouldRenderTooltip: show,
      xPos: -1000,
      yPos: -1000
    });
  }

  /**
   * Position absolute elements next to its target
   *
   * @private
   */
  calcPlacement() {
    const { index, isRunning, standaloneData, shouldRenderTooltip } = this.state;
    const { steps, tooltipOffset } = this.props;
    const step = standaloneData || (steps[index] || {});
    logger({
      type: `joyride:calcPlacement${this.getRenderStage()}`,
      msg: ['step:', step],
      debug: this.props.debug,
    });
    const displayTooltip = standaloneData ? true : shouldRenderTooltip;
    const target = this.getStepTargetElement(step);
    if (!target) {
      this.setState({
        shouldRedraw: false
      });
      return;
    }

    const placement = {
      x: -1000,
      y: -1000
    };

    if (step && (standaloneData || (isRunning && steps[index]))) {
      const offsetX = nested.get(step, 'style.beacon.offsetX') || 0;
      const offsetY = nested.get(step, 'style.beacon.offsetY') || 0;
      const position = this.calcPosition(step);
      const body = document.body.getBoundingClientRect();
      const component = this.getElementDimensions(displayTooltip ? '.joyride-tooltip' : '.joyride-beacon');
      const rect = target.getBoundingClientRect();

      // Calculate x position
      if (/^left/.test(position)) {
        placement.x = rect.left - (displayTooltip ? component.width + tooltipOffset : (component.width / 2) + offsetX);
      }
      else if (/^right/.test(position)) {
        placement.x = (rect.left + rect.width) - (displayTooltip ? -tooltipOffset : (component.width / 2) - offsetX);
      }
      else {
        placement.x = rect.left + ((rect.width / 2) - (component.width / 2));
      }

      // Calculate y position
      if (/^top/.test(position)) {
        placement.y = (rect.top - body.top) - (displayTooltip ? component.height + tooltipOffset : (component.height / 2) + offsetY);
      }
      else if (/^bottom/.test(position)) {
        placement.y = (rect.top - body.top) + (rect.height - (displayTooltip ? -tooltipOffset : (component.height / 2) - offsetY));
      }
      else {
        placement.y = (rect.top - body.top);
      }

      if (/^bottom|^top/.test(position)) {
        if (/left/.test(position)) {
          placement.x = rect.left - (displayTooltip ? tooltipOffset : component.width / 2);
        }
        else if (/right/.test(position)) {
          placement.x = rect.left + (rect.width - (displayTooltip ? component.width - tooltipOffset : component.width / 2));
        }
      }

      this.setState({
        shouldRedraw: false,
        xPos: this.preventWindowOverflow(Math.ceil(placement.x), 'x', component.width, component.height),
        yPos: this.preventWindowOverflow(Math.ceil(placement.y), 'y', component.width, component.height)
      });
    }
  }

  /**
   * Update position for small screens.
   *
   * @private
   * @param {Object} step
   *
   * @returns {string}
   */
  calcPosition(step) {
    const { shouldRenderTooltip, standaloneData } = this.state;
    const { tooltipOffset } = this.props;
    const displayTooltip = standaloneData ? true : shouldRenderTooltip;
    const body = document.body.getBoundingClientRect();
    const target = this.getStepTargetElement(step);
    const component = this.getElementDimensions((displayTooltip ? '.joyride-tooltip' : '.joyride-beacon'));
    const rect = target.getBoundingClientRect();
    let position = step.position || STEP_DEFAULTS.position;

    if (/^left/.test(position) && rect.left - (component.width + tooltipOffset) < 0) {
      position = 'top';
    }
    else if (/^right/.test(position) && (rect.left + rect.width + (component.width + tooltipOffset)) > body.width) {
      position = 'bottom';
    }

    return position;
  }

  /**
   * Get the render stage.
   *
   * @private
   * @returns {string}
   */
  getRenderStage() {
    const { shouldRedraw, xPos } = this.state;

    if (shouldRedraw) {
      return ':redraw';
    }
    else if (xPos < 0) {
      return ':pre-render';
    }

    return '';
  }

  /**
   * Prevent tooltip to render outside the window
   *
   * @private
   * @param {Number} value - The axis position
   * @param {String} axis - The Axis X or Y
   * @param {Number} elWidth - The target element width
   * @param {Number} elHeight - The target element height
   * @returns {Number}
   */
  preventWindowOverflow(value, axis, elWidth, elHeight) {
    const winWidth = window.innerWidth;
    const body = document.body;
    const html = document.documentElement;
    const docHeight = Math.max(body.scrollHeight, body.offsetHeight, html.clientHeight, html.scrollHeight, html.offsetHeight);
    let newValue = value;

    if (axis === 'x') {
      if (value + elWidth >= winWidth) {
        newValue = winWidth - elWidth - 15;
      }
      else if (value < 15) {
        newValue = 15;
      }
    }
    else if (axis === 'y') {
      if (value + elHeight >= docHeight) {
        newValue = docHeight - elHeight - 15;
      }
      else if (value < 15) {
        newValue = 15;
      }
    }

    return newValue;
  }

  /**
   * Create a React Element
   *
   * @private
   * @returns {boolean|ReactComponent}
   */
  createComponent() {
    const { index, shouldRedraw, shouldRenderTooltip, standaloneData, xPos, yPos } = this.state;
    const {
      disableOverlay,
      holePadding,
      locale,
      showBackButton,
      showOverlay,
      showSkipButton,
      showStepsProgress,
      steps,
      type
    } = this.props;
    const currentStep = standaloneData || steps[index];
    const step = { ...currentStep };

    const target = this.getStepTargetElement(step);
    const cssPosition = target ? target.style.position : null;
    const shouldShowOverlay = standaloneData ? false : showOverlay;
    const buttons = {
      primary: locale.close
    };

    let component;

    logger({
      type: `joyride:createComponent${this.getRenderStage()}`,
      msg: [
        'component:', shouldRenderTooltip || standaloneData ? 'Tooltip' : 'Beacon',
        'animate:', xPos > -1 && !shouldRedraw,
        'step:', step
      ],
      debug: this.props.debug,
      warn: !target,
    });

    if (!target) {
      return false;
    }

    if (shouldRenderTooltip || standaloneData) {
      const position = this.calcPosition(step);

      if (!standaloneData) {
        if (['continuous', 'guided'].indexOf(type) > -1) {
          buttons.primary = locale.last;

          if (steps[index + 1]) {
            if (showStepsProgress) {
              let next = locale.next;
              if (typeof locale.next === 'string') {
                next = (<span>{locale.next}</span>);
              }
              buttons.primary = (<span>{next} <span>{`${(index + 1)}/${steps.length}`}</span></span>);
            }
            else {
              buttons.primary = locale.next;
            }
          }

          if (showBackButton && index > 0) {
            buttons.secondary = locale.back;
          }
        }

        if (showSkipButton) {
          buttons.skip = locale.skip;
        }
      }

      component = React.createElement(Tooltip, {
        animate: xPos > -1 && !shouldRedraw,
        buttons,
        cssPosition,
        disableOverlay,
        holePadding,
        position,
        selector: sanitizeSelector(step.selector),
        showOverlay: shouldShowOverlay,
        step,
        standalone: Boolean(standaloneData),
        target,
        type,
        xPos,
        yPos,
        onClick: this.onClickTooltip,
        onRender: this.onRenderTooltip
      });
    }
    else {
      component = React.createElement(Beacon, {
        cssPosition,
        step,
        xPos,
        yPos,
        onTrigger: this.onClickBeacon,
        eventType: step.type || 'click'
      });
    }

    return component;
  }

  render() {
    const { index, isRunning, standaloneData } = this.state;
    const { steps } = this.props;
    const hasStep = Boolean(steps[index]);
    let component;
    let standaloneComponent;

    if (isRunning && hasStep) {
      logger({
        type: `joyride:render${this.getRenderStage()}`,
        msg: ['step:', steps[index]],
        debug: this.props.debug,
      });
    }
    else if (!isRunning && standaloneData) {
      logger({
        type: 'joyride:render',
        msg: ['tooltip:', standaloneData],
        debug: this.props.debug,
      });
    }

    if (standaloneData) {
      standaloneComponent = this.createComponent();
    }
    else if (isRunning && hasStep) {
      component = this.createComponent();
    }

    return (
      <div className="joyride">
        {component}
        {standaloneComponent}
      </div>
    );
  }
}

export default Joyride;
