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
            string script = GetSmartSelectorScript();

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
        /// Gets the smart selector JavaScript for website-specific adaptations
        /// </summary>
        private string GetSmartSelectorScript()
        {
            return $@" (function() {{ 
                if (window.cdpRecorderListenersAttached) return; 
                window.cdpRecorderListenersAttached = true; 
                console.log('Injecting Smart Selector CDP Listeners...'); 
                
                // Keep track of input changes to record values
                const inputValues = new Map();

                // Website-specific adaptations
                function detectWebsite() {{
                    const url = window.location.href;
                    const host = window.location.hostname;
                    
                    // Detect TodoMVC
                    if (url.includes('todomvc.com')) {{
                        return 'todomvc';
                    }}
                    
                    // Add more website detections here
                    if (host.includes('ucare.org')) {{
                        return 'ucare';
                    }}
                    
                    return 'generic';
                }}
                
                function getSel(el) {{ 
                    if (!el || !el.tagName) return null; 
                    
                    try {{ 
                        const website = detectWebsite();
                        
                        // TodoMVC specific selectors
                        if (website === 'todomvc') {{
                            // For TodoMVC, prioritize these selectors
                            if (el.className && typeof el.className === 'string') {{
                                // Check for common TodoMVC classes
                                if (el.className.includes('new-todo')) return {{ type: 'ClassName', value: 'new-todo' }};
                                if (el.className.includes('toggle')) return {{ type: 'ClassName', value: 'toggle' }};
                                if (el.className.includes('destroy')) return {{ type: 'ClassName', value: 'destroy' }};
                                
                                // For other classes, just use the first class
                                const classNames = el.className.split(' ').filter(c => c);
                                if (classNames.length > 0) return {{ type: 'ClassName', value: classNames[0] }};
                            }}
                        }}
                        
                        // UCare specific selectors
                        if (website === 'ucare') {{
                            // Check for atomic components
                            if (el.tagName.toLowerCase().startsWith('atomic-')) {{
                                // Get ARIA attributes first for accessibility
                                const ariaLabel = el.getAttribute('aria-label');
                                if (ariaLabel) return {{ type: 'aria-label', value: ariaLabel }};
                                
                                // Try placeholder for input fields
                                const placeholder = el.getAttribute('placeholder');
                                if (placeholder) return {{ type: 'placeholder', value: placeholder }};
                                
                                // Fallback to tag name and class
                                return {{ type: 'TagName', value: el.tagName.toLowerCase() }};
                            }}
                        }}
                        
                        // Generic selectors (for all websites)
                        
                        // Data test attributes are most reliable for testing
                        const testId = el.getAttribute('data-test-id') || el.getAttribute('data-testid') || el.getAttribute('data-test');
                        if (testId) return {{ type: 'data-test-id', value: testId }};
                        
                        // ID is unique and reliable
                        if (el.id) return {{ type: 'Id', value: el.id }};
                        
                        // Accessibility attributes
                        const ariaLabel = el.getAttribute('aria-label');
                        if (ariaLabel && ariaLabel.length < 50) return {{ type: 'aria-label', value: ariaLabel }};
                        
                        // Input placeholders
                        const placeholder = el.getAttribute('placeholder');
                        if (placeholder && placeholder.length < 30) return {{ type: 'placeholder', value: placeholder }};
                        
                        // Web component part attribute
                        const part = el.getAttribute('part');
                        if (part) return {{ type: 'part', value: part }};
                        
                        // For inputs, use type + name
                        if (el.tagName === 'INPUT') {{
                            const name = el.getAttribute('name');
                            if (name) return {{ type: 'name', value: name }};
                            
                            const inputType = el.getAttribute('type') || 'text';
                            if (inputType !== 'text') return {{ type: 'TagName', value: `input[type='${inputType}']` }};
                        }}
                        
                        // Use className for elements with distinct classes
                        if (el.className && typeof el.className === 'string') {{
                            const classes = el.className.split(' ')
                                .filter(c => c && !c.includes(':') && !c.includes('(') && c.length < 20);
                            if (classes.length > 0) return {{ type: 'ClassName', value: classes[0] }};
                        }}
                        
                        // Fallback to tagName
                        return {{ type: 'TagName', value: el.tagName.toLowerCase() }}; 
                    }} 
                    catch (e) {{ 
                        console.error('Error getting selector', e);
                        return {{ type: 'TagName', value: el.tagName ? el.tagName.toLowerCase() : 'unknown' }}; 
                    }}
                }}

                // Get the actual input element, even if inside shadow DOM or custom element
                function getInputElement(el) {{
                    // Is this already an input element?
                    if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.tagName === 'SELECT') {{
                        return el;
                    }}
                    
                    // Check for input inside this element
                    const inputs = el.querySelectorAll('input, textarea, select');
                    if (inputs && inputs.length > 0) {{
                        return inputs[0];
                    }}
                    
                    // Check shadow DOM if available
                    if (el.shadowRoot) {{
                        const shadowInputs = el.shadowRoot.querySelectorAll('input, textarea, select');
                        if (shadowInputs && shadowInputs.length > 0) {{
                            return shadowInputs[0];
                        }}
                    }}
                    
                    // Couldn't find a specific input
                    return null;
                }}

                // Monitor all input and change events to track input values
                document.addEventListener('input', function(ev) {{
                    const t = ev.target;
                    if (!t || !t.tagName) return;
                    
                    // Store the value for the element
                    inputValues.set(t, t.value);
                    
                    // Also track value for parent elements in case of custom components
                    let parent = t.parentElement;
                    while (parent && parent !== document.body) {{
                        inputValues.set(parent, t.value);
                        parent = parent.parentElement;
                    }}
                }}, true);
                
                function rec(ev) {{ 
                    const t = ev.target; 
                    if (!t || !t.tagName || t.tagName==='HTML' || t.tagName==='BODY') return; 
                    
                    // Try to find the actual input element if this is a container
                    const inputEl = getInputElement(t);
                    const value = inputEl ? inputEl.value : (inputValues.get(t) || t.value);
                    
                    // Get smart selector based on website and element
                    const selector = getSel(t);
                    
                    let a = {{ 
                        type: ev.type, 
                        selector: selector.type,  // This is now the selector type (Id, ClassName, etc.)
                        selectorValue: selector.value, // This is the actual selector value
                        value: value,
                        key: ev.key, 
                        tagName: t.tagName, 
                        elementType: t.type 
                    }}; 
                    
                    console.log(`JS Send: type=${a.type}, tag=${a.tagName}, elType=${a.elementType}, sel=${a.selector}=${a.selectorValue}, val=${a.value}`); 
                    
                    try {{ 
                        // Handle change events for all element types
                        if (ev.type==='change') {{
                            window.{_sessionManager.BindingName}(JSON.stringify(a));
                        }}
                        // Handle clicks
                        else if (ev.type==='click') {{ 
                            a.value = inputValues.get(t) || null; // Try to get stored value
                            a.key = null; 
                            window.{_sessionManager.BindingName}(JSON.stringify(a)); 
                        }}
                        // Handle Enter key on ANY element (not just INPUTs)
                        else if (ev.type==='keydown' && ev.key==='Enter') {{ 
                            a.type = 'enterkey'; 
                            // Use stored value or get from input element if available
                            a.value = inputValues.get(t) || (inputEl ? inputEl.value : t.value);
                            window.{_sessionManager.BindingName}(JSON.stringify(a)); 
                        }}
                        // Handle form submissions
                        else if (ev.type==='submit') {{
                            a.type = 'submit';
                            window.{_sessionManager.BindingName}(JSON.stringify(a));
                        }}
                    }} catch(e){{ 
                        console.error('JS Bind Err:', e); 
                    }} 
                }} 

                // Listen for all events that might indicate user actions
                document.addEventListener('click', rec, {{ capture: true, passive: true }}); 
                document.addEventListener('change', rec, {{ capture: true, passive: true }}); 
                document.addEventListener('keydown', rec, {{ capture: true, passive: true }}); 
                document.addEventListener('submit', rec, {{ capture: true, passive: true }});
                
                // Detect form submissions by listening to all forms
                document.querySelectorAll('form').forEach(form => {{
                    form.addEventListener('submit', rec, {{ capture: true }});
                }});
                
                // Watch for dynamically added forms
                new MutationObserver(mutations => {{
                    for (const mutation of mutations) {{
                        if (mutation.type === 'childList') {{
                            mutation.addedNodes.forEach(node => {{
                                if (node.tagName === 'FORM') {{
                                    node.addEventListener('submit', rec, {{ capture: true }});
                                }}
                                if (node.querySelectorAll) {{
                                    node.querySelectorAll('form').forEach(form => {{
                                        form.addEventListener('submit', rec, {{ capture: true }});
                                    }});
                                }}
                            }});
                        }}
                    }}
                }}).observe(document.body, {{ childList: true, subtree: true }});
                
                console.log('Smart Selector CDP Listeners Attached.'); 
            }})();";
        }
    }
}