import * as Y from 'yjs'
import * as awarenessProtocol from 'y-protocols/awareness.js'
import { yCollab } from 'y-codemirror.next'
import { EditorView } from "codemirror";
import { EditorState, Compartment, StateField, StateEffect} from "@codemirror/state"

//import CodeMirror extensions
import { javascript } from '@codemirror/lang-javascript'
import { python } from '@codemirror/lang-python'
import { css } from '@codemirror/lang-css'
import { html } from '@codemirror/lang-html'
import { oneDark } from '@codemirror/theme-one-dark'
import { duotoneLight, duotoneDark} from "@uiw/codemirror-theme-duotone";
import { insertTab, indentWithTab } from '@codemirror/commands';
import {keymap, highlightSpecialChars, drawSelection, highlightActiveLine, dropCursor,
        rectangularSelection, crosshairCursor,
        lineNumbers, highlightActiveLineGutter, Decoration } from "@codemirror/view"
import {defaultHighlightStyle, syntaxHighlighting, indentOnInput, bracketMatching,
        foldGutter, foldKeymap, indentUnit} from "@codemirror/language"
import {defaultKeymap, history, historyKeymap} from "@codemirror/commands"
import {searchKeymap, highlightSelectionMatches, openSearchPanel } from "@codemirror/search"
import {autocompletion, completionKeymap, closeBrackets, closeBracketsKeymap} from "@codemirror/autocomplete"
import {lintKeymap} from "@codemirror/lint"


self.Y = Y;
self.awarenessProtocol = awarenessProtocol;
self.yCollab = yCollab;
self.EditorView = EditorView;
self.EditorState = EditorState;
self.Compartment = Compartment;
self.StateField = StateField;
self.StateEffect = StateEffect;
self.Decoration= Decoration;

self.javascript = javascript;
self.python = python;
self.css = css;
self.html = html;
self.oneDark = oneDark;
self.duotoneDark = duotoneDark;
self.duotoneLight = duotoneLight;
self.insertTab = insertTab;
self.indentWithTab = indentWithTab;
self.keymap = keymap;
self.highlightSpecialChars = highlightSpecialChars;
self.drawSelection = drawSelection;
self.highlightActiveLine = highlightActiveLine;
self.dropCursor = dropCursor;
self.rectangularSelection = rectangularSelection;
self.crosshairCursor = crosshairCursor;
self.lineNumbers = lineNumbers;
self.highlightActiveLineGutter = highlightActiveLineGutter;
self.defaultHighlightStyle = defaultHighlightStyle;
self.syntaxHighlighting = syntaxHighlighting;
self.indentOnInput = indentOnInput;
self.bracketMatching = bracketMatching;
self.foldGutter = foldGutter;
self.foldKeymap = foldKeymap;
self.indentUnit = indentUnit;
self.defaultKeymap = defaultKeymap;
self.cmhistory = history;//conflicting name with other JS library
self.historyKeymap = historyKeymap;
self.searchKeymap = searchKeymap;
self.highlightSelectionMatches = highlightSelectionMatches;
self.openSearchPanel = openSearchPanel;
self.autocompletion = autocompletion;
self.completionKeymap = completionKeymap;
self.closeBrackets = closeBrackets;
self.closeBracketsKeymap = closeBracketsKeymap;
self.lintKeymap = lintKeymap;

