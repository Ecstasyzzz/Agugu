#target photoshop

const aguguXmpNamespace = "http://www.agugu.org/";
const aguguNamespacePrefix = "agugu:";

const xmpConfigRootTag = "Config";
const xmpLayersRootTag = "Layers";
const xmpLayerIdTag = "Id";
const xmpLayerPropertyRootTag = "Properties";

var config = {};

function getAllLayerInfos(doc){
    var result = [];
    for (var i = 0; i < doc.layers.length; i++)
    {
        var layer = doc.layers[i];
        var layerInfo = extractLayerInfo(layer, 0);
        result.push(layerInfo);

        getLayerNameRecursive(result, layer, 1);
    }
    return result;
}

function extractLayerInfo(layer, indent){
    var layerInfo = {};

    layerInfo.name = layer.name;
    layerInfo.id = layer.id;
    layerInfo.indent = indent;

    return layerInfo;
}

function getLayerNameRecursive(result, targetlayer, indent){
    if( targetlayer.layers == undefined ){ return; }

    for (var i = 0; i < targetlayer.layers.length; i++)
    {
        var layer = targetlayer.layers[i];
        var layerInfo = extractLayerInfo(layer, indent);
        result.push(layerInfo)

        getLayerNameRecursive(result, layer, indent + 1);
    }
}

function setLayerConfig(config, layerId, propertyName, propertyValue){
    if(config[layerId] == undefined){
        config[layerId] = {};
    }

    config[layerId][propertyName] = propertyValue;
}

function clearLayerConfig(config, layerId){
    config[layerId] = {}
}

// XMP
function loadConfigFromXMP(config, xmp){
    var configRoot = xmp.getProperty(aguguXmpNamespace, xmpConfigRootTag);
    if( configRoot == undefined ) { return; }

    var layersRootPath = XMPUtils.composeStructFieldPath(aguguXmpNamespace, xmpConfigRootTag, 
                                                         aguguXmpNamespace, xmpLayersRootTag);
    var layersRoot = xmp.getProperty(aguguXmpNamespace, layersRootPath);
    if( layersRoot == undefined ) { return; }

    var layerCount = xmp.countArrayItems(aguguXmpNamespace, layersRootPath);
    for(var i = 1; i <= layerCount; i++){
        var layerPath = XMPUtils.composeArrayItemPath(aguguXmpNamespace, layersRootPath, i);
        var layerIdPath = XMPUtils.composeStructFieldPath(aguguXmpNamespace, layerPath,
                                                          aguguXmpNamespace, xmpLayerIdTag);
        var layerId = xmp.getProperty(aguguXmpNamespace, layerIdPath).value;

        var layerPropertiesPath = XMPUtils.composeStructFieldPath(aguguXmpNamespace, layerPath,
                                                                  aguguXmpNamespace, xmpLayerPropertyRootTag);
        var propertyIter = xmp.iterator(XMPConst.ITERATOR_JUST_CHILDREN , aguguXmpNamespace, layerPropertiesPath);
        var layerProperty = propertyIter.next();
        while( layerProperty != null){
            var propertyPath = layerProperty.path;
            var propertyName = getLeafPropertyName(propertyPath, aguguNamespacePrefix);
            var propertyValue = xmp.getProperty(aguguXmpNamespace, propertyPath).value;

            setLayerConfig(config, layerId, propertyName, propertyValue);
            layerProperty = propertyIter.next();
        }
    }
}

function getLeafPropertyName(xmpPath, namespacePrefix){
    var pathElements = xmpPath.split("/");
    var pathElementsCount = pathElements.length;
    var leafPropertyNameWithNamespace = pathElements[pathElementsCount-1];
    return leafPropertyNameWithNamespace.replace(namespacePrefix, "")
}

function saveConfigToXMP(config, xmp){
    xmp.deleteProperty(aguguXmpNamespace, xmpConfigRootTag);
    xmp.setProperty(aguguXmpNamespace, xmpConfigRootTag, null, XMPConst.PROP_IS_STRUCT);
    var layersRootPath = XMPUtils.composeStructFieldPath(aguguXmpNamespace, xmpConfigRootTag, 
                                                         aguguXmpNamespace, xmpLayersRootTag);
    xmp.setProperty(aguguXmpNamespace, layersRootPath, null, XMPConst.PROP_IS_ARRAY);

    var xmpLayerArrayIndex = 1;
    for(var layerId in config){
        var layerPath = XMPUtils.composeArrayItemPath(aguguXmpNamespace, layersRootPath, xmpLayerArrayIndex);
        xmp.setProperty(aguguXmpNamespace, layerPath, null, XMPConst.PROP_IS_STRUCT);
        var layerIdPath = XMPUtils.composeStructFieldPath(aguguXmpNamespace, layerPath,
                                                          aguguXmpNamespace, xmpLayerIdTag);
        xmp.setProperty(aguguXmpNamespace, layerIdPath, layerId);

        var layerPropertyRootPath = XMPUtils.composeStructFieldPath(aguguXmpNamespace, layerPath,
                                                                    aguguXmpNamespace, xmpLayerPropertyRootTag);
        var layerConfig = config[layerId];
        for(var propertyName in layerConfig){
            var propertyValue = layerConfig[propertyName];
            
            var propertyPath = XMPUtils.composeStructFieldPath(aguguXmpNamespace, layerPropertyRootPath,
                                                               aguguXmpNamespace, propertyName);
            xmp.setProperty(aguguXmpNamespace, propertyPath, propertyValue);
        }
        
        xmpLayerArrayIndex += 1;
    }
    
    app.activeDocument.xmpMetadata.rawData = xmp.serialize();
}


// UI Panel
function appendLayerList(appendTarget){
    // TreeView is not supported by Photoshop CC 2018
    appendTarget.layerList = appendTarget.add(
        "ListBox{}"
    );
 
    var allLayerInfos = getAllLayerInfos(activeDocument);
    for(layerId in allLayerInfos)
    {
        var layerInfo = allLayerInfos[layerId];
        
        var paddedIdString = padLayerIdString(layerInfo.id);
        var indentString = Array(layerInfo.indent + 1).join("   ");
        var listItemText = paddedIdString + indentString + layerInfo.name;
        
        var listItem = appendTarget.layerList.add ("item", listItemText);
        listItem.layerId = layerInfo.id;
    }
    
    appendTarget.layerList.onChange = function(){
        var selectedLayerListItem = appendTarget.layerList.selection;
        if(selectedLayerListItem == undefined){ return; }
        
        var selectedLayerId = selectedLayerListItem.layerId;
        updateLayerStatusLabel(selectedLayerId);
    }
}

function padLayerIdString(id){
    if(id <= 9){
        return "   " + id + " ";
    }else if(id <= 99){
        return "  " + id + " ";
    }else if(id <= 999){
        return " " + id + " ";
    }else{
        return "" + id + " ";
    }
}


function appendCurrentLayerControls(appendTarget){
    var group = appendTarget.add("Group{orientation: 'row'}");
    
    appendTarget.currentSelectedLayerNameText = group.add(
        "StaticText{\
            text: 'Select',\
            bounds: [0, 0, 400, 100]\
            properties: { multiline: true, scrolling: true }\
    }");
    
    var clearButton = group.add("Button{text:'Clear'}");
    clearButton.onClick = createClearLayerCallback();
}

function createClearLayerCallback(){
    return function(){
        var selectedLayerListItem = mainWindow.layerGroup.layerList.selection;
        if(selectedLayerListItem == undefined){ return; }
        
        var selectedLayerId = selectedLayerListItem.layerId;
        
        clearLayerConfig(config, selectedLayerId);
        
        updateLayerStatusLabel(selectedLayerId);
    }
}


function appendSkipPanel(appendTarget){
    var skipPanel = appendTarget.add(
        "Panel{\
            text: 'Skip',\
            orientation: 'row',\
            \
            skipButton: Button{ text: 'Skip' },\
            unskipButton: Button{ text: 'Unskip'}\
        }"
    );
    
    skipPanel.skipButton.onClick = createSkipLayerCallback(true);
    skipPanel.unskipButton.onClick = createSkipLayerCallback(false);
}

function createSkipLayerCallback(isSkipped){
    return function(){
        var selectedLayerListItem = mainWindow.layerGroup.layerList.selection;
        if(selectedLayerListItem == undefined){ return; }
        
        var selectedLayerId = selectedLayerListItem.layerId;
        
        setLayerConfig(config, selectedLayerId, 'isSkipped', isSkipped);
        
        updateLayerStatusLabel(selectedLayerId);
    }
}


// Widget Type
const widgetType = ['image', 'text'];
function appendWidgetPanel(appendTarget){
    var widgetTypePanel = appendTarget.add(
        "Panel{\
            text: 'Widget',\
            orientation: 'row'\
        }"
    );

    for(var w = 0; w< widgetType.length; w++){
        var widget = widgetType[w];
        var widgetButton = widgetTypePanel.add("button", undefined, widget);
        widgetButton.onClick = createWidgetButtonCallback(widget);
    }
}

function createWidgetButtonCallback(widgetType){
    return function(){
        var selectedLayerListItem = mainWindow.layerGroup.layerList.selection;
        if(selectedLayerListItem == undefined){ return; }
        
        var selectedLayerId = selectedLayerListItem.layerId;
        
        setLayerConfig(config, selectedLayerId, 'widgetType', widgetType);
        updateLayerStatusLabel(selectedLayerId);
    }
}


// Anchor Type
const xAnchorType = ['left', 'center', 'right', 'stretch'];
const yAnchorType = ['top', 'middle', 'bottom', 'stretch'];
function appendAnchorPanel(appendTarget){
    var anchorPanel = appendTarget.add("panel", undefined, "Anchor");

    var scriptFolder = Folder($.fileName).path;
    
    for(var y = 0; y < yAnchorType.length; y++){
        var yAnchor = yAnchorType[y];
        
        var horizontalGroup = anchorPanel.add('group');
        horizontalGroup.add("StaticText { text: '" + yAnchor + "', preferredSize:[50,20], justify: 'center'}")
        for(var x = 0; x < xAnchorType.length; x++){
            var xAnchor = xAnchorType[x];
            var iconPath = scriptFolder + "/UIImage/" + xAnchor + "-" + yAnchor+ ".png";
            var icon = File(iconPath) ;
            
            var anchorButton;
            if(y == 0){
                var verticalGroup = horizontalGroup.add("Group{orientation:'column'}");
                verticalGroup.add("StaticText { text: '" + xAnchor + "', justify: 'center'}");
                anchorButton = verticalGroup.add("iconbutton", undefined, ScriptUI.newImage(icon,icon,icon,icon));
            }else{
                anchorButton = horizontalGroup.add("iconbutton", undefined, ScriptUI.newImage(icon,icon,icon,icon));
            }
            anchorButton.onClick = createAnchorButtonCallback(xAnchor, yAnchor)
         }
    }
}

function createAnchorButtonCallback(xAnchor, yAnchor){
    return function(){
        var selectedLayerListItem = mainWindow.layerGroup.layerList.selection;
        if(selectedLayerListItem == undefined){ return; }
        
        var selectedLayerId = selectedLayerListItem.layerId;
        
        setLayerConfig(config, selectedLayerId, 'xAnchor', xAnchor);
        setLayerConfig(config, selectedLayerId, 'yAnchor', yAnchor);
        
        updateLayerStatusLabel(selectedLayerId);
    }
}


function appendPivotPanel(appendTarget){
    var pivotPanel = appendTarget.add(
        "Panel{\
            text: 'Pivot',\
            orientation: 'column',\
            \
            pivotXLabel: StaticText { text: 'Pivot X' },\
            pivotXText: EditText { text: '0.5' },\
            \
            pivotYLabel: StaticText { text: 'Pivot Y' },\
            pivotYText: EditText { text: '0.5' },\
            \
            addPivotButton: Button{ text: 'Add Pivot'}\
        }"
    );
    
    pivotPanel.addPivotButton.onClick = createPivotButtonCallback(pivotPanel.pivotXText, pivotPanel.pivotYText);
}

function createPivotButtonCallback(xPivotText, yPivotText){
    return function(){
        var selectedLayerListItem = mainWindow.layerGroup.layerList.selection;
        if(selectedLayerListItem == undefined){ return; }
        
        var selectedLayerId = selectedLayerListItem.layerId;
        
        setLayerConfig(config, selectedLayerId, 'xPivot', xPivotText.text);
        setLayerConfig(config, selectedLayerId, 'yPivot', yPivotText.text);
        
        updateLayerStatusLabel(selectedLayerId);
    }
}


function appendSerializePanel(appendTarget){
    var serializePanel = appendTarget.add(
        "Panel{\
            text: 'Serialize',\
            \
            serializeButton: Button { text: 'Serialize' }\
        }"
    );

    serializePanel.serializeButton.onClick = function(){
        saveConfigToXMP(config, xmp);
    };
}


function updateLayerStatusLabel(selectedLayerId){
    mainWindow.optionGroup.currentSelectedLayerNameText.text = getLayerStatusText(selectedLayerId);
}

function getLayerStatusText(selectedLayerId) {
    var showText = "";
    var layerConfig = config[selectedLayerId];
    
    if( layerConfig != undefined && layerConfig.length != 0){
        for(var property in layerConfig){
            showText += (property + " : " + layerConfig[property]);
            showText += "\n";
        }
    }
    return showText;
}


// Load XMPMeta reference
if(ExternalObject.AdobeXMPScript == undefined) {
    ExternalObject.AdobeXMPScript = new ExternalObject('lib:AdobeXMPScript');
}

var xmp = new XMPMeta(activeDocument.xmpMetadata.rawData);
XMPMeta.registerNamespace(aguguXmpNamespace, aguguNamespacePrefix);

loadConfigFromXMP(config, xmp);


// Build Window
var mainWindow = new Window (
"palette {\
    text: 'Agugu',\
    orientation: 'row',\
    frameLocation: [400,160],\
    \
    layerGroup: Group{},\
    optionGroup: Group{ orientation: 'column' }\
}");

        
        
appendLayerList(mainWindow.layerGroup);

appendCurrentLayerControls(mainWindow.optionGroup);

var skipWidgetGroup = mainWindow.optionGroup.add("Group{}");
appendSkipPanel(skipWidgetGroup);
appendWidgetPanel(skipWidgetGroup);

var anchorPivorGroup = mainWindow.optionGroup.add("Group{}");
appendAnchorPanel(anchorPivorGroup);
var pivotSerializeGroup = anchorPivorGroup.add("Group{orientation:'column'}");
appendPivotPanel(pivotSerializeGroup);
appendSerializePanel(pivotSerializeGroup);



// Start main event loop
isDone = false;

mainWindow.onClose = function() {
  return isDone = true;
};

mainWindow.show();

while(isDone === false){
    try{
        app.refresh();
    }catch(e){
         if ( e.number != 8007 ) {
             alert( e + " : " + e.line );
         }else{
             isDone = true;
         }
    }
}