using System;
using System.Threading.Tasks;
using OpenQA.Selenium.DevTools.V136.Page;
using OpenQA.Selenium.DevTools.V136.Runtime;
using WebDriverCdpRecorder.Browser;
using WebDriverCdpRecorder.Utils;

namespace WebDriverCdpRecorder.Core
{
    /// <summary>
    /// Handles injection of JavaScript code for event monitoring
    /// </summary>
    public class JavaScriptInjector
    {
        private readonly DevToolsSessionManager _sessionManager;

        /// <summary>
        /// Constructor
        /// </summary>
        public JavaScriptInjector(DevToolsSessionManager sessionManager)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        /// <summary>
        /// Injects listener script into the page
        /// </summary>
        public async Task InjectListeners()
        {
            if (_sessionManager.Domains == null)
            {
                Logger.Log("ERROR: Cannot inject JavaScript - DevTools domains not available");
                return;
            }

            Logger.Log("Injecting JavaScript listeners...");
            string script = GetInjectionScript();

            try
            {
                // Add script to evaluate on new document loads
                await _sessionManager.Domains.Page.AddScriptToEvaluateOnNewDocument(
                    new AddScriptToEvaluateOnNewDocumentCommandSettings { Source = script });
                
                // Evaluate script on current document
                await _sessionManager.Domains.Runtime.Evaluate(
                    new EvaluateCommandSettings { Expression = script, Silent = false });
                
                Logger.Log("SUCCESS: JavaScript injection completed.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error injecting script: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the JavaScript code to inject
        /// </summary>
        private string GetInjectionScript()
        {
            string bindingName = _sessionManager.BindingName;
            
            return @"(function() {
                // Avoid reinitializing if already done
                if (window.cdpRecorderListenersAttached) {
                    return;
                }
                
                console.log('Attaching CDP recorder listeners');
                window.cdpRecorderListenersAttached = true;
                
                // Track input values
                const inputValues = new Map();
                
                // Find the best selector for an element
                function getBestSelector(el) {
                    if (!el || !el.tagName) return null;
                    
                    try {
                        // Try data-test attributes first (best practice for testing)
                        const testId = el.getAttribute('data-test-id') || 
                                     el.getAttribute('data-testid') || 
                                     el.getAttribute('data-test');
                        if (testId) {
                            return { type: 'data-test-id', value: testId };
                        }
                        
                        // ID is unique and reliable
                        if (el.id) {
                            return { type: 'Id', value: el.id };
                        }
                        
                        // ARIA attributes for accessibility
                        const ariaLabel = el.getAttribute('aria-label');
                        if (ariaLabel && ariaLabel.length < 50) {
                            return { type: 'aria-label', value: ariaLabel };
                        }
                        
                        // Placeholder for inputs
                        const placeholder = el.getAttribute('placeholder');
                        if (placeholder && placeholder.length < 30) {
                            return { type: 'placeholder', value: placeholder };
                        }
                        
                        // Name attribute for form elements
                        const name = el.getAttribute('name');
                        if (name) {
                            return { type: 'name', value: name };
                        }
                        
                        // Class name as fallback
                        if (el.className && typeof el.className === 'string') {
                            const classes = el.className.split(' ')
                                .filter(c => c && c.length > 0 && !c.includes(':'));
                            
                            if (classes.length > 0) {
                                return { type: 'ClassName', value: classes[0] };
                            }
                        }
                        
                        // Tag name as final fallback
                        return { type: 'TagName', value: el.tagName.toLowerCase() };
                    }
                    catch (e) {
                        console.error('Error getting selector:', e);
                        return { type: 'TagName', value: el.tagName ? el.tagName.toLowerCase() : 'unknown' };
                    }
                }
                
                // Find input element in container or shadow DOM
                function findInputElement(el) {
                    if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.tagName === 'SELECT') {
                        return el;
                    }
                    
                    const inputs = el.querySelectorAll('input, textarea, select');
                    if (inputs && inputs.length > 0) {
                        return inputs[0];
                    }
                    
                    if (el.shadowRoot) {
                        const shadowInputs = el.shadowRoot.querySelectorAll('input, textarea, select');
                        if (shadowInputs && shadowInputs.length > 0) {
                            return shadowInputs[0];
                        }
                    }
                    
                    return null;
                }
                
                // Track input values as they change
                document.addEventListener('input', function(e) {
                    const target = e.target;
                    if (!target || !target.tagName) return;
                    
                    inputValues.set(target, target.value);
                    
                    let parent = target.parentElement;
                    while (parent && parent !== document.body) {
                        inputValues.set(parent, target.value);
                        parent = parent.parentElement;
                    }
                }, true);
                
                // Handle user actions
                function handleEvent(e) {
                    const target = e.target;
                    if (!target || !target.tagName || target.tagName === 'HTML' || target.tagName === 'BODY') {
                        return;
                    }
                    
                    try {
                        const inputEl = findInputElement(target);
                        const value = inputEl ? inputEl.value : (inputValues.get(target) || target.value);
                        const selector = getBestSelector(target);
                        
                        if (!selector) return;
                        
                        let action = {
                            type: e.type,
                            selector: selector.type,
                            selectorValue: selector.value,
                            value: value,
                            key: e.key,
                            tagName: target.tagName,
                            elementType: target.type
                        };
                        
                        // Process different event types
                        if (e.type === 'change') {
                            window['" + bindingName + @"'](JSON.stringify(action));
                        }
                        else if (e.type === 'click') {
                            action.value = inputValues.get(target) || null;
                            window['" + bindingName + @"'](JSON.stringify(action));
                        }
                        else if (e.type === 'keydown' && e.key === 'Enter') {
                            action.type = 'enterkey';
                            action.value = inputValues.get(target) || (inputEl ? inputEl.value : target.value);
                            window['" + bindingName + @"'](JSON.stringify(action));
                        }
                    }
                    catch (error) {
                        console.error('Event handling error:', error);
                    }
                }
                
                // Add event listeners
                document.addEventListener('click', handleEvent, { capture: true, passive: true });
                document.addEventListener('change', handleEvent, { capture: true, passive: true });
                document.addEventListener('keydown', handleEvent, { capture: true, passive: true });
                
                console.log('CDP Recorder initialized successfully');
            })();";
        }
    }
}