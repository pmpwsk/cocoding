//get set up iframe contents
var iFrameContents;
fetch("./IFrame.html")
.then(response => {
   if (!response.ok) {
       throw new Error(`Failed to load file: ${response.statusText}`);
   } 
   return response.text();
})
    .then(text => {
        iFrameContents = text;
    })
//set the iframe and run the code
function compileJS(codeToRun){
    var contents;
    //check for compile errors
    //if error -> content=error
    //otherwise content=codeToRun
    try {
        Babel.transform(codeToRun, {
            presets: ['env'],
        });
        contents = getContentWithCode(codeToRun);
    } catch (error){
        contents = printError(error);
    }
    const iFrame = document.getElementById("iFrame");
    
    //set timeout otherwise some browsers have issues rerendering the iFrame
    setTimeout(() => {
        iFrame.srcdoc = contents;
    }, 0);
}

function compilePY(codeToRun){
    const escapedCode = codeToRun.replace(/\\/g, '\\\\');
    const embeddedCode = `
    async function main() {
            console.log("Lade Imports, bitte warten...");
            const originalConsoleLog = console.log;
            console.log = () => {};
            const pyodide = await loadPyodide();

            var code = \`${escapedCode}\`;
          
            await pyodide.loadPackagesFromImports(code);
            console.log = originalConsoleLog;
            try{
                console.empty();
                await pyodide.runPythonAsync(\`${escapedCode}\`)
            } catch(e){
                console.error(e)
            }
        }

        main();

    `
    const scripts = ["https://cdn.jsdelivr.net/pyodide/v0.27.0/full/pyodide.js"];
    const contents = getContentWithCode(embeddedCode, scripts);
    
    const iFrame = document.getElementById("iFrame");

    //set timeout otherwise some browsers have issues rerendering the iFrame
    setTimeout(() => {
        iFrame.srcdoc = contents;
    }, 0);
}

//returns the contents of the set up iframe + the code passed
function getContentWithCode(codeToRun, scripts=[]) {
    var prePreFix = iFrameContents.split("<!--SCRIPT_DELIMITER-->")[0];
    const postPreFix = iFrameContents.split("<!--SCRIPT_DELIMITER-->")[1];
    scripts.forEach(script => {
        prePreFix += `<script src="${script}"></script>\n`;
    })
    const preFix = postPreFix.split("//DELIMITER")[0];
    const postFix = postPreFix.split("//DELIMITER")[1];
    return prePreFix + preFix + "\n" + codeToRun + "\n" + postFix;
}

//returns the contents of the set up iframe + an error message
function printError(error){
    setNewLine('error', null, [error.message]);
}

//receive messages from within the iframe to dynamically resize it
window.addEventListener("message", (event) => {
    switch (event.data.type) {
        case "log":
            const args = event.data.args;
            if (args.length === 0){
                setNewLine("", null,  [""]);
                return;
            }
            const deserializedArray = [];
            
            var iteratorCounter = 0;

            //if not using console.dir printf the arguments
            if (event.data.mode !== 'dir' && args[0].type==='string' && args[0].data.includes("%")) {
                const mappedArray = args.map(e => e.data)
                deserializedArray.push(sprintf(...mappedArray));
                iteratorCounter += args[0].data.split("%").length;
            }
            for (var i = iteratorCounter; i < args.length; i++){
                deserializedArray.push(deserialize(args[i]));
            };
            setNewLine(event.data.style, null, deserializedArray);
            break;
        case "setGroup":
            setNewGroup(event.data.name, event.data.collapsed);
            break;
        case "endGroup":
            if (level === 0)
                return;
            level--;
            parentDiv = parentDiv.parentNode;
            break;
        case 'clearConsole':
            document.getElementById('compLines').innerHTML = '';
            break;
        case 'table':
            var deserializedTableArray = [];
            const tableArgs = event.data.args;
            try {

                tableArgs[0].data.forEach(arg => {
                    deserializedTableArray.push(JSON.parse(arg.contents));
                })
                if (tableArgs.length > 1) {
                    var columns = deserialize(event.data.args[1]);
                    if (!(typeof columns === "string") || isNaN(columns) || isNaN(parseInt(columns))) 
                        createTable(deserializedTableArray);
                    else
                        createTable(deserializedTableArray, parseInt(columns));
                } else
                    createTable(deserializedTableArray);
            }catch{
                deserializedTableArray = [];
                tableArgs.forEach(arg => {
                    deserializedTableArray.push(deserialize(arg));
                })
                if (tableArgs.length === 0){
                    setNewLine("", null,  [""]);
                    return;
                }
                else
                    setNewLine(event.data.style, null, deserializedTableArray);
            }
            break;
            
    }
});
function deserialize(arg) {

    if (arg.type === "string" || arg.type === "unknown")
        return arg.data;
    else if (arg.type === "CoCoDiNgfunction")
        return arg;
    else if (arg.type === "CoCoDiNgobject" || arg.type === "CoCoDiNgmap") {
            return arg;
    }
    else if (arg.type === "array" || arg.type === "set") {
        const tempArray = [];
        arg.data.forEach(item => {
            tempArray.push(deserialize(item));
        });
        if (arg.type === "set")
            return new Set(tempArray);
        else
            return tempArray;
    }
}

//set up functions needed for console methods
var parentDiv;
var level;

//scroll down to the last line of the compiler
function scrollDown(){
    const lastChild = compLines.lastElementChild;
    if (lastChild)
        lastChild.scrollIntoView();
}

//returns a div dependent on the content passed
//checks for different types like Array, Map, string, numbers...
function getLineContent(content, style="") {
    const lineContent = document.createElement('div');
    //set style of the content
    lineContent.className = "compiler-line-content " + style;
    if (content === null || content === undefined) {
        lineContent.textContent = "undefined";
        return lineContent;
    }
    //handle strings and numbers
    if (typeof (content) === "string") {
        lineContent.textContent = content;
        return lineContent;
    }
    //handle functions
    if (content.type === "CoCoDiNgfunction") {
        lineContent.textContent = content.data;
        lineContent.style.fontStyle = "italic";
        return lineContent;
    }

    //handle Arrays and Sets -> recursively get contained elements
    if (Array.isArray(content) ||
        content instanceof Set) {
        //contains the header and the contents
        const containerDiv = document.createElement('div');
        containerDiv.style.display = "flex";
        containerDiv.style.flexDirection = "column";

        var length;
        var prefix;
        var postfix;
        //make header
        var header = document.createElement('div');
        header.className = 'compiler-line';
        var toggleElement = document.createElement('div');
        toggleElement.className = 'toggle-terminal collapsed';
        header.appendChild(toggleElement);
        header.appendChild(lineContent);

        if (Array.isArray(content)) {
            lineContent.textContent = `Array(${content.length})`;
            length = content.length;
            prefix = '[';
            postfix = ']';
        } else {
            lineContent.textContent = `Set(${content.size})`;
            length = content.size;
            prefix = '{';
            postfix = '}';
        }
        containerDiv.appendChild(header);

        //if there is something inside the element signify it is clickable
        if (length > 0) {
            header.style.cursor = "pointer";
            lineContent.className = "compiler-line-content group";
        } else {
            lineContent.className = "compiler-line-content " + style;
            return lineContent;
        }
        //line containing the actual elements
        const contentDiv = document.createElement('div');
        contentDiv.className = "compiler-line";
        contentDiv.style.display = "none";
        containerDiv.appendChild(contentDiv);

        //make header clickable;
        header.onclick = function () {
            if (contentDiv.style.display === "flex") {
                contentDiv.style.display = "none";
                toggleElement.className = 'toggle-terminal collapsed';
                return;
            }
            contentDiv.style.display = "flex";
            toggleElement.className = 'toggle-terminal expanded';
            lineContent.scrollIntoView();
        }

        var contentContainer = document.createElement('div');
        contentContainer.className = 'compiler-line';
        var toggleEmpty = document.createElement('div');
        toggleEmpty.className = 'toggle-empty';
        contentContainer.appendChild(toggleEmpty);
        contentContainer.appendChild(getLineContent(prefix, style))

        //append the actual contents
        contentDiv.appendChild(contentContainer);
        //handle case when array has "holes" in it
        //else just get elements recursively
        if (Array.isArray(content) && content.filter(item => item !== undefined).length !== length) {
            for (var index = 0; index < length; index++) {
                if (content[index] === undefined)
                    contentDiv.appendChild(getLineContent("undef", style));
                else
                    contentDiv.appendChild(getLineContent(content[index], style));
                if (index < length - 1)
                    contentDiv.appendChild(getLineContent(","));
            }
        } else {
            var counter = 0;
            content.forEach((item) => {
                contentDiv.appendChild(getLineContent(item, style));
                if (counter < length - 1)
                    contentDiv.appendChild(getLineContent(","));
                counter++;
            });
        }
        contentDiv.appendChild(getLineContent(postfix, style));
        return containerDiv;
    }

    //handle objects
    if (content.type === "CoCoDiNgobject" || content.type === "CoCoDiNgmap") {
        //the container holding the lineContent (==type of content)
        //and the groupContents (==result of JSON.stringify())
        const groupContainer = document.createElement('div');
        groupContainer.style.display = "flex";
        groupContainer.style.flexDirection = "column";
        //this div holds the result of JSON.stringify()
        const groupContents = document.createElement('div');
        var header = document.createElement('div');
        header.className = 'compiler-line';
        var toggleElement = document.createElement('div');
        toggleElement.className = 'toggle-terminal collapsed';
        header.appendChild(toggleElement);
        header.appendChild(lineContent);
        //set header to show cursor as clickable 
        header.style.cursor = "pointer";
        //make color blue, to show the line is clickable
        lineContent.className = "compiler-line-content group";
        //set lineContent to type of object
        lineContent.textContent = content.header;
        groupContainer.appendChild(header);
        groupContainer.appendChild(groupContents);
        //hide the contents of the object
        groupContents.style.display = "none";
        //make contents visible when lineContent is clicked
        header.onclick = function () {
            if (groupContents.style.display === "block") {
                groupContents.style.display = "none";
                toggleElement.className = 'toggle-terminal collapsed';
                return;
            }
            groupContents.style.display = "block";
            toggleElement.className = 'toggle-terminal expanded';
            groupContainer.scrollIntoView();
        }
        const objContainer = document.createElement('div');
        objContainer.className = 'compiler-line';
        var toggleEmpty = document.createElement('div');
        toggleEmpty.className = 'toggle-empty';
        objContainer.appendChild(toggleEmpty);

        //create another div actually holding the stringified data
        //so we can add a filler div in front
        const objContents = document.createElement('div');
        objContents.textContent = content.contents;
        objContents.className = "compiler-line-content " + style;

        objContainer.appendChild(objContents)

        //add filler div and contents to groupContents
        groupContents.appendChild(getFillerDiv());
        groupContents.appendChild(objContainer);
        return groupContainer;
    }
}
//adds a new group to current parent and increments the indentation
function setNewGroup(groupname="<Gruppe ohne Namen>", collapsed){

    var toggleElement = document.createElement('div');
    if (collapsed)
        toggleElement.className = 'toggle-terminal collapsed';
    else
        toggleElement.className = 'toggle-terminal expanded';

    var nameArray = [];
    nameArray.push(groupname);
    //the line showing the group name
    const groupLine = setNewLine("group", toggleElement, nameArray);
    //show different cursor to signify line is clickable
    groupLine.style.cursor = "pointer";
    //container for the content of the group 
    const groupContainer = document.createElement('div');
    groupContainer.style.flexDirection = "column"
    groupContainer.style.display = collapsed ? "none" : "flex";
    //toggle visibility when clicking on groupLine
    groupLine.onclick = function(){
        if (groupContainer.style.display === "flex") {
            groupContainer.style.display = "none";
            toggleElement.className = 'toggle-terminal collapsed';
            return;
        }
        groupContainer.style.display="flex";
        toggleElement.className = 'toggle-terminal expanded';
        groupLine.scrollIntoView();
    }
    //increment indentation
    level++;
    parentDiv.appendChild(groupContainer);
    //make the groupContainer the new parent
    parentDiv = groupContainer;
}
//filler div to create indentation
function getFillerDiv(showBorder=false){
    const fillerDiv = document.createElement('div');
    fillerDiv.className = "compiler-line-content filler-div";
    if (showBorder){
        fillerDiv.style.borderLeft = 'solid';
        fillerDiv.style.borderLeftColor = "var(--text-lower)";
    }
    return fillerDiv;
}

//creates a table from the given data (has to be passed as an array of objects
function createTable(data, columns = Number.MAX_VALUE){

    // Create table element
    const table = document.createElement('table');
    table.style.borderCollapse = "collapse";
    const compDiv = document.getElementById("compDiv");
    table.style.width = compDiv.offsetWidth - 10*level - 10 + "px";
    table.style.marginTop = "10px";
    table.style.marginBottom = "10px";
    table.style.overflow = "visible";

    // Create table header
    const headerRow = document.createElement('tr');
    headerRow.style.backgroundColor = "#4b4b4b";
    headerRow.style.color = "#f8f9fa";
    headerRow.style.textAlign = "left";

    //collect keys for header
    const keys = new Set();
    keys.add("Index");
    data.forEach(item => {
        Object.keys(item).forEach(key => {
            if (columns >=0 && keys.size < columns + 1)
                keys.add(key);
        });
    });
    //insert them
    keys.forEach(col => {
        const th = document.createElement('th');
        th.textContent = col;
        th.style.border = "1px solid var(--line)";
        th.style.padding = "8px";
        th.style.background = "var(--background-colored)";
        th.style.color = "var(--text)";
        headerRow.appendChild(th);
    });
    table.appendChild(headerRow);

    var counter = 0;
    // Create table rows
    data.forEach(item => {
        const row = document.createElement('tr');
        row.style.borderBottom = "1px solid var(--line)";

        keys.forEach(col => {
            const td = document.createElement('td');
            if (col === "Index")
                td.textContent = counter;
            else
                td.textContent = item[col] || "";
            td.style.border = "1px solid var(--line)";
            td.style.padding = "8px";
            td.style.background = "var(--background-plain)";
            td.style.color = "var(--text)";
            row.appendChild(td);
        });
        table.appendChild(row);
        counter++;
    });

    const tableLine = document.createElement('div');
    tableLine.className = 'compiler-line';
    tableLine.style.overflowX = "auto";
    for (var i=0; i<level;i++)
        tableLine.appendChild(getFillerDiv());
    tableLine.appendChild(table);

    parentDiv.appendChild(tableLine);
    scrollDown();
}

//inserts a new line into the current parent
//iterates through arguments and appends them as 
//content to the line
function setNewLine(style="",toggleElement=null, args){
    const newLine = document.createElement('div');
    newLine.className = 'compiler-line';
    for (var i = 0; i < level; i++)
        newLine.appendChild(getFillerDiv(true));

    if (toggleElement !== null)
        newLine.appendChild(toggleElement);

    var iteratorCounter = 0;

    for (var i=iteratorCounter; i<args.length;i++) {
        newLine.appendChild(getLineContent(args[i], style));
        newLine.appendChild(getFillerDiv());
    }
    parentDiv.appendChild(newLine);
    scrollDown();
    return newLine;
}

//get test cases
function getJSTestCases() {
return `
console.warn('Text nur sichtbar, wenn Funktion console.clear nicht funktioniert!');
console.clear();
// Object
var obj = new Object();
console.dir(obj); //Expected: [object Object] {}
obj.name = "Test";
console.dir(obj); //Expected: [object Object] {"name": "Test" }

// Array
var arr = new Array(5);
console.dir(arr); //Expected: Array(5) [ undef , undef, undef , undef , undef ]
arr[0] = 42; 
console.dir(arr); //Expected: Array(5) [ 42 , undef, undef , undef , undef ]

// Function
var func = new Function('a', 'b', 'return a + b;');
console.dir(func); //Expected: function anonymous (italic)
console.log(func(2, 3)); //Expected: 5

// Date
var date = new Date();
console.dir(date); //Expected: Current Date object
console.log(date.toISOString()); //Expected: Current Date object as ISO string

// RegExp
var regex = new RegExp('\\\\d+');
console.dir(regex); // Expected: /\\\\d+/ 
console.log(regex.test('123')); //Expected: true
console.log(regex.test('k')); //Expected: false

// Error
var error = new Error('Something went wrong');
console.dir(error); //Expected: Error: Something went wrong
console.log(error.message); //Expected: Something went wrong

// Map
var map = new Map();
console.dir(map); //Expected: [object Map] {}
map.set('key', 'value');
console.dir(map); //Expected: [object Map] { "key": "value" }

// Set
var set = new Set();
console.dir(set); //Expected: Set(0)
set.add(42, 99); 
console.dir(set); //Expected: Set(2) { 42, 99 } 

// Boolean (Object Wrapper)
var bool = new Boolean(true);
console.dir(bool); //Expected: true
console.log(bool.valueOf()); //Expected: true

// Number (Object Wrapper)
var num = new Number(42);
console.dir(num); //Expected: 42
console.log(num.valueOf()); //Expected: 42

// String (Object Wrapper)
var str = new String('hello');
console.dir(str); //Expected: hello
console.log(str.valueOf()); //Expected: hello

// Symbol (Cannot Use \`new\`)
try {
    var sym = new Symbol('test');
} catch (error) {
    console.dir(error); //Expected: TypeError: Symbol is not a constructor
}

// Custom Class Example
class Custom {
    constructor(value) {
        this.value = value;
    }
}
var custom = new Custom(42);
console.dir(custom); // Expected: Custom { "value": 42 }

// Test timeStamp and time
console.timeStamp('Timertests');
console.time('Timertests');
console.time("Timertests2");

// Data for Testing
var list = [1,2,3,4];
var map = new Map();map.set('name', 'name');map.set('age', 36);map.set('list', list);

// Test colours
console.log('Testlog');
console.warn('Testwarn');
console.error('Testerror');
console.info('Testinfo');
console.debug('Testdebug');
// Test string replacement
` +
"var a=1; console.log(`${ a }`);" +`
console.log('%d', 1);

var text = "";
var i = 0;
while (i < 5) {
  text += "Die Zahl ist " + i +", ";
  i++;
}

console.log(text);
// BigInt
console.log(99999n);

// Test groups
console.group('Testgroup0');
console.log("Group 0, Item 0");
console.log("Group 0, Item 1");
console.groupCollapsed('Testgroup1 (collapsed)');
console.log("Group 1, Item 0");
console.log("Group 1, Item 1");
console.log(map, map);
console.groupEnd();
console.log("Group 0, Item2");
console.groupEnd();

// Timer result
console.timeLog('Timertests');

// Test dir
console.dir(list);
console.dir(map);
console.dir('%d', 1, map);
console.dir({ Name: 'Bob', Age: 30, Job: 'Designer' });
console.dir([1,2,3,4]);

// Test table  
//Expected: only 2 columns shown
console.table([{Name: "Peter", Age: 31, Job: "Developer"},{Name: "Peter", Age: 31, Talent: "Talking"},{Age: 21, Name: "Angelina", Talent: "Singing"}],2);
//Expected: all columns shown
console.table([{Name: "Peter", Age: 31, Job: "Developer"},{Name: "Peter", Age: 31, Talent: "Talking"},{Age: 21, Name: "Angelina", Talent: "Singing"}]);

// Test count
console.count("Testzähler");
console.count("Testzähler");
console.count("Testzähler1");
console.countReset("Testzähler");
console.count("Testzähler");
console.count("Testzähler1");

// Timer results
console.timeLog("Timertests2");
console.timeEnd('Timertests');
console.assert(i == 5, "Fehlerhafte Bedingung i==5 !");
console.assert(i == 4, "Fehlerhafte Bedingung i==4 !");
console.timeEnd("Timertests2");
addNumbers(result, 10);

// Test Promise
var promise = new Promise((resolve) => resolve('done'));
promise.then(console.log); //Expected: done

`;

}

function getPYTestCases() {
return `
import logging
import numpy.core
import re
import time


# Print
print('Welcome to Cocoding!') # Expected: Welcome to Cocoding!

# Logging
print("->Logging:")
logging.basicConfig(level=logging.DEBUG)
logging.info("Info message") # Expected: INFO:root:Info message
logging.debug("Debug message") # Expected: DEBUG:root:Debug message
logging.warning("Warning message") # Expected: WARNING:root:Warning message
logging.error("Error message") # Expected: ERROR:root:Error message

# Date
print("->Test date and time:")
from datetime import datetime
date = datetime.now()
print(date)  # Expected: Current date and time

# Timer
print("->Test Timer:")
start_time = time.time()
time.sleep(1)  # 1 second delay
end_time = time.time()
print(f"Elapsed time: {end_time - start_time:.2f} Seconds")  # Expected: approx. 1 Second

# Basic Arithmetic
print("->Test Arithmetic:")
a = 10
b = 20
print(f"Sum: {a + b}") # Expected: Sum: 30
print(f"Difference: {b - a}") # Expected: Difference: 10
print(f"Product: {a * b}") # Expected: Product: 200
print(f"Division: {b / a}") # Expected: Division: 2

# For Loop
print("->Test for loop:")
for i in range(5):
    if i % 2 == 0:
        print(f"{i} is even")
    else:
        print(f"{i} is odd") # Expected: 0 is even
      						 #			 1 is odd
      						 #			 2 is even
      						 #  		 3 is odd
      						 #			 4 is even

# Function
print("->Test function:")
def add(a, b):
    return a + b
print(add(2, 3))  # Expected: 5

# Lambda Functions
print("->Test lambda function:")
add = lambda x, y: x + y
print(add(5, 3))  # Expected: 8

# Recursion
print("->Test function and recursion:")
def factorial(n):
    if n == 0:
        return 1
    return n * factorial(n - 1)
print(factorial(5)) # Expected: 120

# Regular Expression
print("->Test regular expression:")
text = "The rain in Spain"
pattern = r"\\bS\\w+"
match = re.findall(pattern, text)
print(match) # Expected: ['Spain']

# Array
print("->Test array:")
arr = [None] * 5
print(arr)  # Expected: [None, None, None, None, None]
arr[0] = 42
print(arr)  # Expected: [42, None, None, None, None]

# Array Using Numpy
print("->Test Array using numpy.core:")
array = numpy.array([1, 2, 3, 4, 5])
result = array.sum()
print(result)  # Expected: 15
print(array * 2) # Expected: [2 4 6 8 10]

# List Comprehension
print("List comprehension:")
squares = [x ** 2 for x in range(10)]
print(squares) # Expected: [0, 1, 4, 9, 16, 25, 36, 49, 64, 81]

# Groups
print("->Test groups:")
group_0 = ["Group 0, Item 0", "Group 0, Item 1"]
group_1 = ["Group 1, Item 0", "Group 1, Item 1"]
print("Testgroup 0:")
for item in group_0:
    print(item)  # Expected: Group 0, Item 0
  				 #			 Group 0, Item 1
print("Testgroup 1:")
for item in group_1:
    print(item)  # Expected: Group 1, Item 0
  				 #			 Group 1, Item 1

# Set
print("->Test set:")
my_set = {42}
print(my_set)  # Expected: {42}
my_set.add(99)
print(my_set)  # Expected: {42, 99}

# Dictionary Manipulation
print("->Test dictionary manipulation:")
data = {"name": "Angelika", "age": 25, "city": "Vienna"}
print(data) # Expected: {'name': 'Angelika', 'age': 25, 'city': 'Vienna'}
data["country"] = "Austria"
print(data) # Expected: {'name': 'Angelika', 'age': 25, 'city': 'Vienna', 'country': 'Austria'}

# Table
print("->Test Table:")
data = [
    {'Name': 'Peter', 'Alter': 31, 'Job': 'Entwickler'},
    {'Name': 'Peter', 'Alter': 31, 'Talent': 'Sprechen'},
    {'Name': 'Angelina', 'Alter': 21, 'Talent': 'Singen'}
]
print("all colums:")
for row in data:
    print(row)  # Expected:  {'Name': 'Peter', 'Alter': 31, 'Job': 'Entwickler'}
				#            {'Name': 'Peter', 'Alter': 31, 'Talent': 'Sprechen'}
				#			 {'Name': 'Angelina', 'Alter': 21, 'Talent': 'Singen'}
print("only two colums:")
for row in data:
    print({key: row[key] for key in list(row.keys())[:2]}) # Expected: {'Name': 'Peter', 'Alter': 31}
														   #	  	   {'Name': 'Peter', 'Alter': 31}
														   # 		   {'Name': 'Angelina', 'Alter': 21}


# Class Definition
print("->Class defintion:")
class Person:
    def __init__(self, name, age):
        self.name = name
        self.age = age
    def greet(self):
        return f"Hallo, Ich bin {self.name} und ich bin {self.age} Jahre alt."
p = Person("Ole", 47)
print(p.greet())  # Expected: Hallo, Ich bin Ole und ich bin 47 Jahre alt.

# File I/O (Simulated)
print("->File I/O simulated:")
with open("test.txt", "w") as f:
    f.write("Hello from Python!")
with open("test.txt", "r") as f:
    content = f.read()
print(content)  # Expected: Hello from Python!

# Error Handling
print("->Error handling:")
try:
    result = 10 / 0
except ZeroDivisionError as e:
    logging.error(f"Error occurred: {e}") # Expected: ERROR:root:Error occurred: division by zero

`;

}