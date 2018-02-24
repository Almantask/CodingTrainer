﻿(function () {
    // Setting this option, with a parameter of the get-hints function, enables
    // hint loading
    CodeMirror.defineOption("loadHints", false, function (cm, getHintsFunction) {
        if (getHintsFunction) {
            cm.loadedHints = {
                getHints: getHintsFunction,
                generation: null,
                // These "current" variables are all to do with the current token, and will become
                // invalid if the user moves to a different token
                currentToken: null,
                currentLine: null,
                currentHints: null
            };

            // Register key events, etc, that might require the hints to be updated
            cm.on('keyup', function (cm, e) {
                if (e.altKey || e.ctrlKey || e.metaKey) return;
                checkHints(cm, e.key);
            });

            // TO DO - add Ctrl-Space keymap


        } else {
            // TO DO - Remove everything that's been registered
        }
    });
    
    // To be called when hints have been downloaded from the server
    CodeMirror.defineExtension("showHints", function (hints, tokenStart) {
        // Hints are available

        // Is the token that raised these hints is still selected?
        var currentTokenStart = this.indexFromPos({ line: this.loadedHints.currentLine, ch: this.loadedHints.currentToken.start });
        if (tokenStart !== currentTokenStart) return;

        this.loadedHints.currentHints = hints;
        var filtered = filterAndShow(this);
    });


    // This gets called when a change has occured which may require hints to be updated
    function checkHints(cm, key) {
        // Don't hint if code is selected
        if (cm.somethingSelected()) return;

        // Don't hint if the code hasn't changed since last time - the user is just navigating
        // N.B. THIS MAY NEED TO CHANGE WHEN WE IMPLEMENT Ctrl-Space
        if (cm.isClean(cm.loadedHints.generation)) return;
        cm.loadedHints.generation = cm.changeGeneration();

        // These keys can't be indicate that the token is not something we can hint:
        var noToken = ['Enter', ';', ' ', '(', ')', '{', '}', '[', ']'];
        if (noToken.indexOf(key) !== -1) return;

        // Find the current token
        var token = cm.getTokenAt(cm.getCursor());
        console.log(token.string + ' is a ' + token.type + ', and the key was "' + key + '"');
        var line = cm.getCursor().line;

        // Literals don't have hints
        if (token.type === "string" || token.type === "number") return;

        // . requires hints - but not for this token, which is just .
        // The hints will be for the next token
        if (token.string === ".") {
            token.start++;
            token.end = Math.max(token.start, token.end);
        }


        if (cm.loadedHints.currentToken && cm.loadedHints.currentToken.start === token.start && cm.loadedHints.currentLine === line) {
            console.log('Continuing');
            cm.loadedHints.currentToken = token;  // Because the end point will have changed
            if (cm.loadedHints.currentHints) filterAndShow(cm);
        } else {
            // New token - get new hints if necessary
            console.log('New token');
            cm.loadedHints.currentToken = token;
            cm.loadedHints.currentLine = line;

            // Is this the start of a new token?
            if (token.string.length === 1 && token.string === key) {
                console.log('Loading completions');
                // This function will ask the server for the hints, then call showHints when it has the hints
                cm.loadedHints.getHints(cm, cm.indexFromPos({ line: line, ch: token.start }));
            }
        }
    }

    function filterAndShow(cm) {
        var hints = cm.loadedHints.currentHints;
        if (!hints) return;
        var token = cm.getTokenAt(cm.getCursor());
        var filtered;
        var seleceted;

        if (token.string === '.') {
            filtered = hints;
            selected = 0;
        } else {
            var lowerToken = token.string.toLowerCase();
            filtered = [];
            selected = -1;
            for (var i = 0; i < hints.length; i++) {
                var lowerHint = hints[i].toLowerCase();
                if (lowerHint.indexOf(lowerToken) !== -1) {
                    // If the token is a substring of the hint, then include this hint
                    filtered.push(hints[i]);
                    if (lowerHint.indexOf(lowerToken) === 0) {
                        // The first hint we find that starts with the token
                        // the user has typed will be the selected one
                        if (selected === -1) selected = filtered.length - 1;
                    }
                }
            }
            // If we haven't found a hint that starts with the current token, just selected
            // the first item in the list
            if (selected === -1) selected = 0;
        }

        var options = {
            hint: function () {
                return {
                    from: {
                        line: cm.loadedHints.currentLine,
                        ch: cm.loadedHints.currentToken.start
                    },
                    to: {
                        line: cm.loadedHints.currentLine,
                        ch: cm.loadedHints.currentToken.end
                    },
                    list: filtered,
                    selectedHint: selected
                };
            },
            completeSingle: false
        };
        //options.on("pick", function () {
        //    // Need to ensure the user-typed portion gets replaced, not added to

        //});
        cm.showHint(options);
    }
})();
