const JS_STRING = "Javascript";
const PYTHON_STRING = "Python";
const HTML_STRING = "Html";
const CSS_STRING = "Css";

var ydoc;
var update;
var awareness;
var currentLocalState;//keeps track of the currentLocalState of the awareness
var undoManager;
var editorView;
const fileTypeCompartment = new Compartment();//compartment to dynamically set the file type
const readOnlyCompartment = new Compartment();//compartment to toggle readonly/edit mode in editor
const addHighlightEffect = StateEffect.define();//define an effect for adding highlighting to text
const removeHighlightEffect = StateEffect.define();//define an effet for removing highlighting from text
var undoManagerDeletedStack = []; //keeps track of deleted items client and clock id -> needed to update relative positions
var undoingMode = true; //true if the sequence of deletions was done by the undoManager, false if user deletes text manually

var updatesEnabled = true;
var awarenessEnabled = true;
var pushTimer = null;
var editorEnabled = false;
var docLoaded = false;
var readOnly = false;
var searchPanelOpen = false;

var connectionPromise;
var connection;

var settingElementId = null;
var settingUserId = null;
var settingSessionId = null;
var settingUserName = null;
var settingUserColor = null;
var settingFileId = null;
var settingFileType = null;
var settingIndent = null;
var settingDarkMode = null;

var printStatusInformation = true;
var mingleUpdates = false; //used for testing purposes
let testId;//id for test purposes


window.setSettings = function (elementId, userId, sessionId, userName, userColor, fileId, fileType= "Text", indent, darkMode) {
    settingElementId = elementId;
    settingUserId = userId;
    settingSessionId = sessionId;
    settingUserName = userName;
    settingUserColor = userColor;
    settingFileId = fileId;
    settingFileType = fileType;
    settingIndent = indent;
    settingDarkMode = darkMode;
}

//creates an editor and returns the view, returns null if element with elementId does not exist
//will also need to pass userId and possibly userName
window.createEditor = async function () {

    const element = document.getElementById(settingElementId);
    if (!element) {
        //error handling
        if (printStatusInformation)
            console.error("Element not found:", settingElementId);
        return null;
    }

    //set color
    if (printStatusInformation)
        console.log("Color: " + settingUserColor);

    //creates a new YDoc -> this doc is synchronized by applying updates to it
    ydoc = new Y.Doc();

    //defines a YText element in the text -> access via ydoc.getText('editorText')
    //the key allows changes from different docs to be merged with the right map entry
    //this means you can have different data sets and types in one doc
    const ytext = ydoc.getText('editorText')

    //The Yjs undo manager -> is bound to CodeMirror when instance is created via keymap
    undoManager = new Y.UndoManager(ytext, {trackedOrigins: new Set([settingSessionId])});

    //reset the stack variable when the stack of the UndoManager is cleared
    undoManager.on('stack-cleared', (event) =>{
        undoManagerDeletedStack = [];
    });
   
    /*
     * The listener below is necessary because Yjs on redo after deleting text reinserts the content as a new transaction 
     * (this is due to a rather aggressive garbage collection policy, see https://github.com/yjs/yjs/issues/638).
     * This means there are possibly new client/clock ids for existing selections. Therefor we keep track of deletions
     * and on reinsert update existing selections if necessary. 
     */
    
    //listen for deleted text and update client and clock id of relative positions if necessary
    undoManager.on('stack-item-added', async (event) =>{


        //check whether we are inserting
        if (event.stackItem.insertions.clients.size > 0) {
            //if deleted text is reinserted update selections that are affected
            if (event.origin.awareness === undefined) {//if awareness is undefined the event is not a human inserting but the UndoManager
                var i = 0;
                var entry;

                //iterate over insertions
                do {
                    entry = event.stackItem.insertions.clients.entries().next();
                    var stackItem = undoManagerDeletedStack.pop();
                    if (stackItem === undefined)
                        return;
                    await connection.invoke("UpdateSelection", stackItem[0], stackItem[1], entry.value[0], entry.value[1][0].clock, stackItem[2]);
                    i++;
                } while (i < event.stackItem.insertions.clients.size);
            }else{//human entering code
                //loop over insertions and check whether the next index is a selection end, if so update that entry in the database
                //-> this way text added after the selection is not included
                var i = 0;
                var entry;

                //iterate over insertions
                do {
                    //get absolute position from stack entry
                    entry = event.stackItem.insertions.clients.entries().next();
                    var absolutePosition = Y.createAbsolutePositionFromRelativePosition(JSON.parse(getJSON(entry.value[0], entry.value[1][0].clock, -1, -1)).start, ydoc);
                    if (absolutePosition == null)
                        return;
                    //get the next position of the text as a relative position
                    var relativePosition = Y.createRelativePositionFromTypeIndex(ydoc.getText('editorText'), absolutePosition.index+1);

                    //update the selection if necessary
                    if (relativePosition.item == null)
                        await connection.invoke("UpdateSelection", -1, -1, entry.value[0], entry.value[1][0].clock, 1, true);
                    else
                        await connection.invoke("UpdateSelection", relativePosition.item.client, relativePosition.item.clock, entry.value[0], entry.value[1][0].clock, 1, true);
                    i++;
                } while (i < event.stackItem.insertions.clients.size);
                
                
            }
        }
        //else text was modified "by a human" -> all deletions on the stack that have been added by the UndoManager
        //are not redoable and have to be removed. Furthermor we are in a sequence of human actions -> undoingMode==false!
        else {
            //if undoingMode===true clear stack
            if (undoingMode)
                undoManagerDeletedStack = [];
            undoingMode = false;
        }

        //handle deletions -> add them to the undoManagerDeleted Stack
        if (event.stackItem.deletions.clients.size > 0) {
            
            //if the awareness is not defined the deletion was executed by the UndoManager -> we are in a sequence
            //of undo operations -> undoingMode==true!
            if (event.origin.awareness === undefined)
                undoingMode = true
            
            var i = 0;
            var entry;
            //iterate over deletions and add them to the stack
            do {
                entry = event.stackItem.deletions.clients.entries().next();
                undoManagerDeletedStack.push([entry.value[0], entry.value[1][0].clock, entry.value[1][0].len]);
                i++;
            }
            while (i < event.stackItem.deletions.clients.size);
        }
        
    });
    
    //tracks the actions of the cursor and en/decodes them as updates
    //the user information gets passed to the awareness, when the 
    //user interacts with the editor -> see listeners below
    awareness = new awarenessProtocol.Awareness(ydoc);

    //defines the attributes of the cursor
    const localStateField = {
        name: settingUserName,
        color: settingUserColor,
        colorLight: settingUserColor,
        id: settingSessionId
    };

    const awarenessTag = "user";

    // define a function to create decorations for a range -> used to set background of selection
    function createRangeDecoration(from, to, color) {
        return Decoration.mark({
            attributes: { style: `background-color: ${color};` }
        }).range(from, to);
    }
    
    // define a StateField to manage the decorations
    const highlightField = StateField.define({
        // initialize empty
        create() {
            return Decoration.none;
        },

        // apply updates to the decorations
        update(decorations, transaction) {
            for (var effect of transaction.effects) {
                if (effect.is(addHighlightEffect)) {
                    // add new decorations
                    decorations = decorations.update({
                        add: [createRangeDecoration(effect.value.from, effect.value.to, effect.value.color)]
                    });
                } else if (effect.is(removeHighlightEffect)) {
                    // remove decorations for the specified range
                    decorations = decorations.update({
                        filter: (from, to) =>
                            !(from === effect.value.from && to === effect.value.to)
                    });
                }
            }
            return decorations;
        },

        // provide decorations to the EditorView
        provide: field => EditorView.decorations.from(field)
    });

    
    //create instance of CodeMirror and pass setup
    const state = EditorState.create({
        //extensions add functionality to the editor
        //i.e. highlighting, autocomplete etc.
        extensions: [
            readOnlyCompartment.of([]),//used to toggle readOnly/edit mode
            //basic setup, see https://github.com/codemirror/basic-setup/blob/main/src/codemirror.ts
            lineNumbers(),
            highlightActiveLineGutter(),
            highlightSpecialChars(),
            //cmhistory(), //history is turned off, history is managed by Yjs
            foldGutter(),
            drawSelection(),
            dropCursor(),
            EditorState.allowMultipleSelections.of(true),
            indentUnit.of(" ".repeat(settingIndent)),
            indentOnInput(),
            syntaxHighlighting(defaultHighlightStyle, {fallback: true}),
            bracketMatching(),
            closeBrackets(),
            autocompletion(),
            rectangularSelection(),
            crosshairCursor(),
            highlightActiveLine(),
            highlightSelectionMatches(),
            keymap.of([
                ...closeBracketsKeymap,
                ...defaultKeymap,
                ...searchKeymap,
                ...historyKeymap,
                ...foldKeymap,
                ...completionKeymap,
                ...lintKeymap,
                indentWithTab,
                //custom keymap to bind Yjs undo manager to Ctrl-z
                {
                    key: "Ctrl-z",
                    run: () => undoManager.undo(),
                },
                {
                    key: "Ctrl-y",
                    run: () => undoManager.redo(),
                     
                },
                {
                     key: "Ctrl-Shift-F10",
                     run: () => {
                         if ((settingFileType === JS_STRING) || (settingFileType === PYTHON_STRING))
                            compile()}
                      
                },
                {
                    key: 'Tab',
                    preventDefault: true,
                    run: insertTab,
                },
            ]),
            //end basic setup

            //handle awareness for certain events
            EditorView.domEventHandlers({
                //add cursor when editor is focused 
                focus: (event, view) =>  {
                    //if this is the first time the user interacts with the editor
                    //the currentLocalState of the awareness is still undefined
                    //and needs to be created
                    if (currentLocalState === undefined){
                        // Set awareness local state with user information
                        awareness.setLocalStateField(awarenessTag, localStateField);
                        //remember localState
                        currentLocalState = awareness.getLocalState();
                    } else { //else use the existing localState
                        awareness.setLocalState(currentLocalState);
                    }
                },
                //remove cursor when editor loses focus
                blur: (event, view) => {
                    //this tells the other clients state we have
                    //no state at the moment
                    awareness.setLocalState(null);
                },
            }),
            /* activated and changed theme setting from oneDark to duotone-dark and duotone-light */
            settingDarkMode ? duotoneDark : duotoneLight,
            fileTypeCompartment.of(getFileTypeExtension(settingFileType)),
            yCollab(ytext, awareness, { undoManager }),//bind Yjs to CodeMirror; this would actually have provider.awareness instead of 'undefined'; may be somehow substituted with Yjs awarness UInt8Array encoded awareness messaging?
            highlightField,//custom extension being used to dynamically update the background of a given range -> see addHighlight() and removeHighlight()
        ],
    });

    //create a CodeMirror EditorView, pass the above defined state and bind it to the element passed
    element.innerHTML = "";
    editorView = new EditorView({
        state: state,
        parent: element,
    });

    editorEnabled = true;
    
    //listen to changes in the ydoc and transmit them to the other clients 
    ydoc.on('updateV2', async update => {
        //PASS VIA SIGNALR HERE
        if (updatesEnabled) {
            if (printStatusInformation)
                console.log("Sending update: " + update);
            if (mingleUpdates)
                await connection.invoke("PushMingled", update, testId);//this only serves testing purposes
            else
                await connection.invoke("PushUpdate", update, settingSessionId, getLineNumber());
        }
    });

    //spread awareness to other clients
    awareness.on('update', async ({ added, updated, removed }) => {
        if (awarenessEnabled && connection !== undefined && connection.state === "Connected") {
            const changedClients = added.concat(updated).concat(removed);
            if (printStatusInformation)
                console.log("Spreading awareness: " + changedClients);
            const isMobile = window.isMobileDevice();
            if (awarenessEnabled)
                await connection.invoke('BroadcastAwareness', awarenessProtocol.encodeAwarenessUpdate(awareness, changedClients), settingSessionId, getLineNumber());
        }
    });

    //receive ydoc updates from others
    connection.on("ApplyUpdate", update => {
        if (editorEnabled) {
            if (!docLoaded)
                docLoaded = true;
            console.log("Update received: " + update)
            updatesEnabled = false;
            Y.applyUpdateV2(ydoc, update);
            updatesEnabled = true;
        }
    });

    // Receive awareness updates from other clients via SignalR
    connection.on("ReceiveAwareness", (awarenessUpdate) => {
        if (editorEnabled) {
            update = awarenessUpdate;
            if (printStatusInformation)
                console.log("Receiving awareness: "+ awarenessUpdate);
            awarenessEnabled = false;
            awarenessProtocol.applyAwarenessUpdate(awareness, awarenessUpdate);
            awarenessEnabled = true;
        }
    });

    //when a new client connects this request is triggered
    connection.on("BroadcastAwarenessRequest", (issuersession, issuername) => {
        if (printStatusInformation)
            console.log(`BroadcastAwarenessRequest received, issued by ${issuername}`);

        //resetting localStateField to trigger awareness update
        awareness.setLocalStateField(awarenessTag, localStateField);

        /*check whether the issuer is present in the clients awareness 
         * -> normally the issuer wants to receive a broadcast because 
         *  he/she is joining the document anew 
         * -> if the name is already present in awareness.getStates() 
         *  this means the issuer did not broadcast the checkout message
         *  properly, and we see a lingering cursor
         *  this means that there will be multiple cursors for the same user
         *  to prevent that we remove this lingering awareness manually 
         *  and the cursor disappears
         */
        var clients = awareness.getStates();
        clients.forEach((value, key) => {
            if (value.id === issuersession){

                /*disable awareness to not broadcast this change 
                 * -> needed because otherwise the issuer would
                 *  receive an update and delete his/her own state
                */
                awarenessEnabled = false;
                awarenessProtocol.removeAwarenessStates(awareness, [ key ]);
                awarenessEnabled = true;
            }
        });
    });

    connection.on("FileDeleted", () => {
        if (editorEnabled) {
            alert("Die Datei wurde gelöscht!");
            window.location.assign("/");
        }
    });

    await connectionPromise;
    
    //enter file
    await connection.invoke("EnterFile", settingFileId);
    //load document
    await connection.invoke("Load");
    // request awareness of other clients
    await connection.invoke("BroadcastAwarenessRequest", settingSessionId, settingUserName);

    // push state to the server every 30 seconds to keep the state list short
    pushTimer = setInterval(async function() {
        if (connection.state === "Connected") {
            if (printStatusInformation)
                console.log("Pushing state at: " + Date.now());
            await connection.invoke("PushState", Y.encodeStateAsUpdateV2(ydoc));
        }
    }, 30000);

    connection.on("PushState", async () => {
        if (connection.state === "Connected") {
            if (printStatusInformation)
                console.log("Pushing state at: " + Date.now());
            await connection.invoke("PushState", Y.encodeStateAsUpdateV2(ydoc));
        }
    });

    if (printStatusInformation)
        console.log("Push timer started.");

    //adds an overlay and disables the editor
    connection.on("LockEditor", (user=undefined) =>{
        if (!editorEnabled)
            return;
        lockEditor();
        lockOverlay.textContent = "Eine ältere Version des Dokuments wird" + (user ===undefined || user === null? "" : (" von " + user)) + " wiederhergestellt.";
    });
    
    //shows version restored message and reloads the site
    //-> effectively loading the version that has been restored
    connection.on("UnlockEditor", async (date, user, aborted= false) =>{
        if (aborted) {
            alert("Die Wiederherstellung des Dokuments durch " + user + " wurde abgebrochen.")
            lockEditor(false);
            return;
        }
        const options = { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false };
        window.alert(user + " hat eine ältere Version des Dokuments vom " + date.toLocaleDateString('de-DE') + " " + date.toLocaleTimeString('de-DE', options) + " wiederhergestellt.");
        location.reload();
    });
    
    //METHODS FOR TESTING PURPOSES
    /*
    connection.on("ResetEditor", () =>{
        ytext.delete(0,ytext.length);
    });

    connection.on("InsertText", (position, text) => {
        ytext.insert(position, text);
    })
    connection.on("DeleteText", (position1, position2) => {
        ytext.delete(position1, position2);
    })
    connection.on("Console.log", (text) => {
        console.log(text);
    });

    connection.on("SetTestId", (id) => {
        console.log("TestId: " + id)
        testId  = id;
    });

    connection.on("SetIssuerId", async () => {
        await connection.invoke("SetIssuerId", testId);
    });

    connection.on("SetPrintStatusInformation", (print) => {
        printStatusInformation = print;
    });

    connection.on("SetMingledUpdates", (mingle) =>{
        mingleUpdates = mingle;
    });
    //METHODS FOR TESTING END
    */
    
    //undo lock in case we switch files in the sidebar and lockscreen was
    //enabled for another file
    lockEditor(false);
    
    //check whether the file is currently locked
    //if so the EditorHub calls the "LockEditor" method
    await connection.invoke("CheckLocked");

    return editorView;
}

function getLineNumber(){

    if (awareness == null || editorView == null || ydoc == null)
        return -1;

    var localState = awareness.getLocalState();

    if (localState != null && localState.cursor !== undefined) {
        var absoluteStart;
        if (localState.cursor.anchor != null)
            absoluteStart = Y.createAbsolutePositionFromRelativePosition(localState.cursor.anchor, ydoc);
        if (localState.cursor.head != null)
            absoluteStart = Y.createAbsolutePositionFromRelativePosition(localState.cursor.head, ydoc);

        if (absoluteStart != null) {
            var lineObject = editorView.state.doc.lineAt(absoluteStart.index);
            if (lineObject != null)
                return lineObject.number;
        }
    }
    return -1;
}

window.getSelection = function () {

    if(editorView == null)
        return[];
    
    // If there is more than one selection - alert
    
    if(editorView.state.selection.ranges.length > 1)
    {
        alert("Es ist nur eine Markierung erlaubt.")
        return[]; 
    }
    
    var codeMirrorSelection = editorView.state.selection.main;
    var selectionStart = codeMirrorSelection.from;
    var selectionEnd = codeMirrorSelection.to;

    // If there is no selection - alert
    
    if (selectionStart === selectionEnd) 
    {
        alert("Keine Markierung vorhanden!")
        return[];
    }
        
        
    var yText = ydoc.getText("editorText");

    var relativeStart = Y.createRelativePositionFromTypeIndex(yText, selectionStart);
    var relativeEnd = Y.createRelativePositionFromTypeIndex(yText, selectionEnd);

    if(relativeStart === null || relativeEnd === null)
        return[]; 
    
    var clientStart = relativeStart.item === null ? -1 : relativeStart.item.client;
    var clockStart = relativeStart.item === null ? -1 : relativeStart.item.clock;
    var clientEnd = relativeEnd.item === null ? -1 : relativeEnd.item.client;
    var clockEnd = relativeEnd.item === null ? -1 : relativeEnd.item.clock;

    var relativeSelection = [clientStart, clockStart, clientEnd, clockEnd];
    
    return relativeSelection;
    
}

//adds highlight to the provided selection with the given color
function addHighlight(selection, color) {
    if (selection.length !== 4)
        return;
    
    const relativeSelection = JSON.parse(getJSON(selection[0], selection[1], selection[2], selection[3]));
    const absoluteStart = Y.createAbsolutePositionFromRelativePosition(relativeSelection.start, ydoc);
    const absoluteEnd = Y.createAbsolutePositionFromRelativePosition(relativeSelection.end, ydoc);
    if (absoluteStart === null || absoluteEnd === null) return;
    const from = absoluteStart.index;
    const to = absoluteEnd.index;
    if (from === to)
        return;
    editorView.dispatch({
        effects: addHighlightEffect.of({ from , to, color })
    });
}

//remove highlight from selection
function removeHighlight(selection) {
    if (selection.length !== 4)
        return;
        
    const relativeSelection = JSON.parse(getJSON(selection[0], selection[1], selection[2], selection[3]));
    const absoluteStart = Y.createAbsolutePositionFromRelativePosition(relativeSelection.start, ydoc);
    const absoluteEnd = Y.createAbsolutePositionFromRelativePosition(relativeSelection.end, ydoc);
    if (absoluteStart === null || absoluteEnd === null) return;
    const from = absoluteStart.index;
    const to = absoluteEnd.index;
    if (from != null && to != null) {
        editorView.dispatch({
            effects: removeHighlightEffect.of({from, to})
        });
    }
}
function getTextFromUpdates(updates){

    var ydocLocal = new Y.Doc();
    for (upd of updates) 
        Y.applyUpdateV2(ydocLocal, upd);
    return ydocLocal.getText('editorText').toString();
}

async function getTextFromFileId(fileId) {
    return getTextFromUpdates(await connection.invoke("GetFile", fileId));
}

function downloadText(downloadText, fileName) {
    console.log("Downloading file: " + fileName);
    console.log("File content: " + downloadText);
    var downloadBlob = new Blob([downloadText], { type: "text/plain" });
    var downloadLink = document.createElement("a");
    downloadLink.setAttribute("style", "display: none;");
    downloadLink.href = URL.createObjectURL(downloadBlob);
    downloadLink.download = fileName;
    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
    window.URL.revokeObjectURL(downloadLink.href);
}

function downloadThisFile(fileName) {
    downloadText(ydoc.getText('editorText').toString(), fileName);
}

async function downloadFileId(fileId, fileName) {
    downloadText(await getTextFromFileId(fileId), fileName);
}

function getTextSnippet(updates, clientStart, clockStart, clientEnd, clockEnd) {
    var ydocLocal;
    if (updates !== null) {
        ydocLocal = new Y.Doc();

        for (upd of updates) {
            Y.applyUpdateV2(ydocLocal, upd);
        }
    } else
        ydocLocal = ydoc;

    var relativeSelection = JSON.parse(getJSON(clientStart, clockStart, clientEnd, clockEnd));

    var absoluteStart = Y.createAbsolutePositionFromRelativePosition(relativeSelection.start, ydocLocal);
    var absoluteEnd = Y.createAbsolutePositionFromRelativePosition(relativeSelection.end, ydocLocal);
    
    if (absoluteStart === null || absoluteEnd === null) return;

    return ydocLocal.getText("editorText").toString().slice(absoluteStart.index, absoluteEnd.index);
}

window.restoreSelection = function (clientStart, clockStart, clientEnd, clockEnd, scrollIntoView = true) {
    
    if(editorView == null)
        return;
    
    var relativeSelection = JSON.parse(getJSON(clientStart, clockStart, clientEnd, clockEnd)); 
    var absoluteStart = Y.createAbsolutePositionFromRelativePosition(relativeSelection.start, ydoc);
    var absoluteEnd = Y.createAbsolutePositionFromRelativePosition(relativeSelection.end, ydoc); 

    if (absoluteStart === null || absoluteEnd === null) return;
    
    editorView.dispatch({
        selection: {
            anchor: absoluteStart.index, head: absoluteEnd.index
        }
    })

    if (scrollIntoView) {
        editorView.dispatch({
            effects: EditorView.scrollIntoView(absoluteStart.index, {
                y: 'center',
                x: 'start',
            }),
        });
    }


}

function getJSON(clientStart, clockStart, clientEnd, clockEnd) {
    return ` 
    {
        "start":
        {
            "type": null,
            "tname": "editorText",
            ${getJSONItem(clientStart, clockStart)}
            "assoc": 0
        },
        "end":
        {   
            "type": null,
            "tname": "editorText",
            ${getJSONItem(clientEnd, clockEnd)}
            "assoc": 0
         }
    }`
;
}

function getJSONItem(client, clock){
    if (clock === -1)
        return `"item": null,`
    
    return `
        "item":
        {
            "client": ${ client},
            "clock":${ clock}
        },
    `
}


function lockEditor(lock = true) {

    var lockOverlay = document.getElementById("lockOverlay");
    var editor = document.getElementById("editor");
    if (lock) {
        lockOverlay.style.display = "flex";
        editor.style.pointerEvents = "none;"
        editorEnabled = false;
    }else{
        lockOverlay.style.display = "none";
        editor.style.pointerEvents = "default";
        editorEnabled = true;
    }
}

//sets the editors EditorState.readOnly plugin to true (default) or false
window.setReadOnly = function(readOnlyBool = true) {
    if (editorView === undefined)
        return;

    readOnly = readOnlyBool;
    const editorElement = editorView.dom;

    if (readOnly && !searchPanelOpen) {
        editorElement.addEventListener('focus', preventFocus, true);
        editorElement.addEventListener('mousedown', preventFocus, true);
    } else {
        editorElement.removeEventListener('focus', preventFocus, true);
        editorElement.removeEventListener('mousedown', preventFocus, true);
    }

    editorView.dispatch({
        effects: readOnlyCompartment.reconfigure(
            EditorState.readOnly.of(readOnly)
        ),
    });
}
window.setFileType = function(fileType) {
    console.log(fileType)
    if (settingFileType === fileType)
        return;
    console.log("HERE")
    settingFileType = fileType;
    editorView.dispatch({
        effects: fileTypeCompartment.reconfigure(
            getFileTypeExtension(fileType)
        ),
    });
}

function getFileTypeExtension(fileType = settingFileType) {
    switch (fileType) {

        case JS_STRING: return javascript();//set language mode to javascrip
        case PYTHON_STRING: return python(); //set language mode to python
        case CSS_STRING: return css(); //set language mode to CSS
        case HTML_STRING: return html();//set language mode to HTML
    }
    return [];
}
function preventFocus(event) {
    event.preventDefault();
    event.stopImmediatePropagation();
}

window.openSearch = function() {
    if (editorView == null) {
        return false;
    }
    openSearchPanel(editorView);
    
    if (!isMobileDevice())
        return;
    
    searchPanelOpen = true;
    const editorElement = editorView.dom;

    editorElement.removeEventListener('focus', preventFocus, true);
    editorElement.removeEventListener('mousedown', preventFocus, true);
    
    const searchPanel = editorView.dom.querySelector('.cm-search');
    const compDiv = document.getElementById("compDiv");
    
    if (searchPanel != null) {
        const fab = document.getElementById("fab");
        fab.style = "margin-bottom: " + (compDiv.clientHeight + searchPanel.clientHeight) + "px";
        const closeButton= searchPanel.querySelector('[name="close"]');

        // move the fab when search panel resizes
        const obs = new ResizeObserver(() =>{
            const searchPanel = editorView.dom.querySelector('.cm-search');
            const fab = document.getElementById("fab");
            fab.style = "margin-bottom: " + (compDiv.clientHeight + searchPanel.clientHeight) + "px";
        });
        obs.observe(searchPanel);
        
        closeButton.addEventListener('click', ()=> {
            searchPanelOpen = false;
            fab.style = "margin-bottom: " + compDiv.clientHeight + "px";
                obs.unobserve(searchPanel);
            setReadOnly(readOnly);
            //take focus from active element to make keyboard disappear
            if (document.activeElement != null)
                document.activeElement.blur();
        });
    }
    return true;
}
window.isMobileDevice = function () {
    return window.matchMedia("(orientation: portrait) and (pointer: coarse), (orientation: landscape) and (pointer: coarse)").matches;
};


window.onbeforeunload =  () => {disposeConnection(); };

//returns true if the SignalR connection is established
//ensures the server side it is safe to proceed
window.connectionEstablished = function(){
    return connection.state === "Connected";
}


//returns true if the editor is enabled
//ensures the server side it is safe to proceed
window.checkEditorEnabled = function(){
    return editorEnabled;
}

// ensures the ydoc is loaded  
window.checkYdocLoaded = function()
{
 return docLoaded; 
}
    

//saves the documents current state as a version via the EditorHub
window.createVersion = async function(label, comment){
    var error = false;
    
    const errorHandler = (err) => {
        error = true;
        alert(err);
    };
    
    connection.on("Error", errorHandler);
    await connection.invoke("CreateVersion", label, comment, Y.encodeStateAsUpdateV2(ydoc));
    connection.off("Error", errorHandler);
    
    if (!error)
        alert("Die Datei wurde als neue Version gespeichert.")
    return !error;
}

//access the EditorHub in order to propagate the version
//to all clients registered to the given file
window.setVersion = async function(fileId, versionId){
    var error = false;
    connection.on("Error", (err) => {
       alert(err); 
       error = true;
    });
    await connection.invoke("EnterFile", fileId);
    await connection.invoke("SetVersion", versionId);
    await connection.invoke("LeaveFile", fileId);
    return error;
}

//needed to propagate updates to the preview
//since we don't use SignalR
window.applyUpdate = function(update){
    Y.applyUpdateV2(ydoc, update);
}

//creates a preview editorView with the given element
//as a parent, preview meaning that editing and any
//collaborative elements elements are disabled (view-only)
window.preview = function(elementId, fileType, darkMode){

    settingFileType = fileType;
    settingDarkMode = darkMode;
    
    const element = document.getElementById(elementId);
    
    if (!element) {
        //error handling
        if (printStatusInformation)
            console.error("Element not found:", elementId);
        return null;
    }

    //creates a new YDoc 
    ydoc = new Y.Doc();
    
    //defines a YText element in the text -> access via ydoc.getText('editorText')
    const ytext = ydoc.getText('editorText')

    //create dummy UndoManager
    undoManager = new Y.UndoManager(ytext);

    //create dummy awareness with null LocalStateField
    awareness = new awarenessProtocol.Awareness(ydoc);
    awareness.setLocalStateField("user", null);
    
    //create instance of CodeMirror and pass reduced setup
    const state = EditorState.create({
        //extensions add functionality to the editor
        //i.e. highlighting, autocomplete etc.
        extensions: [
            EditorState.readOnly.of(true), //uncomment in case editor should be readonly
            //basic setup, see https://github.com/codemirror/basic-setup/blob/main/src/codemirror.ts
            lineNumbers(),
            highlightActiveLineGutter(),
            highlightSpecialChars(),
            //cmhistory(), //history is turned off, history is managed by Yjs
            foldGutter(),
            drawSelection(),
            dropCursor(),
            EditorState.allowMultipleSelections.of(true),
            indentOnInput(),
            syntaxHighlighting(defaultHighlightStyle, {fallback: true}),
            bracketMatching(),
            closeBrackets(),
            autocompletion(),
            rectangularSelection(),
            crosshairCursor(),
            highlightActiveLine(),
            highlightSelectionMatches(),
            keymap.of([
                ...closeBracketsKeymap,
                ...defaultKeymap,
                ...searchKeymap,
                ...historyKeymap,
                ...foldKeymap,
                ...completionKeymap,
                ...lintKeymap,
            ]),
            //end basic setup

            /* activated and changed theme setting from oneDark to duotone-dark and duotone-light */
            //duotoneDark, //set dark theme
            settingDarkMode ? duotoneDark : duotoneLight,
            settingFileType === JS_STRING ? javascript() : [],//set language mode to javascript
            settingFileType === PYTHON_STRING ?  python() : [], //set language mode to python
            settingFileType === CSS_STRING ? css() : [], //set language mode to CSS
            settingFileType === HTML_STRING ? html() : [], //
            yCollab(ytext, awareness, { undoManager }),//bind Yjs to CodeMirror; this would actually have provider.awareness instead of 'undefined'; may be somehow substituted with Yjs awarness UInt8Array encoded awareness messaging?
        ],
    });

    //create a CodeMirror EditorView, pass the above defined state and bind it to the element passed
    element.innerHTML = "";
    editorView = new EditorView({
        state: state,
        parent: element,
    });
}

//dispose of the editor
window.disposeEditor = async function () {
    editorEnabled = false;
    docLoaded = false;
    
    closeCompiler();

    if (pushTimer !== null) {
        clearInterval(pushTimer);
        pushTimer = null;
        console.log("Push timer stopped.");
    }

    if (connection !== undefined && connection.state === "Connected") {
        await connection.invoke("LeaveFile", settingFileId);
    }

    if (editorView) {
        var container = document.getElementById(settingElementId);
        if (container !== null) {
            container.innerHTML = "Loading...";
        }

        editorView.destroy();
        ydoc.destroy();
        undoManager.destroy();
        awareness.destroy();
        return true;
    } else {
        console.log("CodeMirror instance not found for element: " + settingElementId);
        return false;
    }
}

window.createConnection = function(force = false) {
    if (force || connection === undefined) {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/editor-hub")
            .withAutomaticReconnect()
            .withHubProtocol(new signalRMsgPack.MessagePackHubProtocol())
            .build();
        connection.onreconnecting(async error => {
            if (editorEnabled) {
                await disposeEditor();
                editorEnabled = true;
                console.log(error);
                alert("Verbindung verloren. Der Editor wird neu geladen, wenn die Verbindung wieder vorhanden ist.")
            }
        });
        connection.onreconnected(async cid => {
            if (editorEnabled) {
                await createEditor();
            }
        });
        connectionPromise = connection.start({waitForPageLoad: false});
        connectionPromise.then(() => console.log("Hub connected."));
    }
}

window.disposeConnection = function() {
    if (connection !== undefined) {
        connection.stop().then(() => {
            connection = undefined;
            console.log("Hub disconnected.");
        });
    }
}

window.compile = function() {
    parentDiv = document.getElementById("compLines");
    document.getElementById('compLines').innerHTML = '';
    level = 0;
    if (!editorEnabled)
        return;
    let codeToRun = ydoc.getText('editorText').toString();
    
    switch (settingFileType){
        case JS_STRING: compileJS(codeToRun); break;
        case PYTHON_STRING: compilePY(codeToRun); break;
        default: alert("Die Kompilierung des Dateityps wird leider nicht unterstützt.");return;
    }
    showCompiler();
}
var obs = null;
function showCompiler(){
    const compDiv = document.getElementById('compDiv');
    compDiv.style.display = 'block';
    
    if (!isMobileDevice())
        return;
    
    //add margin for fab and observe compDiv
    const searchPanel = editorView.dom.querySelector('.cm-search');
    const fab = document.getElementById("fab");
    fab.style = "margin-bottom: " + (compDiv.clientHeight + (searchPanel == null ? 0 : searchPanel.clientHeight)) + "px";

    // move the fab when search panel resizes
    obs = new ResizeObserver(() => {
        const searchPanel = editorView.dom.querySelector('.cm-search');
        const fab = document.getElementById("fab");
        fab.style = "margin-bottom: " + (compDiv.clientHeight + (searchPanel == null ? 0 : searchPanel.clientHeight)) + "px";
    });
    
    obs.observe(compDiv);
}

function closeCompiler(){
    const compDiv = document.getElementById('compDiv');
    compDiv.style.display = 'none';
    
    if (!isMobileDevice())
        return;
    
    //remove margin for fab and unobserve compDiv
    const fab = document.getElementById('fab');
    const searchPanel = editorView.dom.querySelector('.cm-search');
    fab.style = "margin-bottom: " + (searchPanel == null ? 0 : searchPanel.clientHeight) + "px";
    if (obs == null)//safety check
        return;
    obs.unobserve(compDiv);
    obs = null;
}

//clears the ydoc and inserts test cases defined in compiler.js
function compileJSTest(){
    const ytext = ydoc.getText('editorText');
    ytext.delete(0,ytext.length);
    ytext.insert(0, getJSTestCases());
}

function compilePYTest() {
    const ytext = ydoc.getText('editorText');
    ytext.delete(0, ytext.length);
    ytext.insert(0, getPYTestCases());
}
