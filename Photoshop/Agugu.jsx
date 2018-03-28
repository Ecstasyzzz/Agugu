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
    var layerListBox = appendTarget.add("ListBox{}");
    
    var allLayerInfos = getAllLayerInfos(activeDocument);
    for(layerId in allLayerInfos)
    {
        var layerInfo = allLayerInfos[layerId];
        var listItem = layerListBox.add ("item", layerInfo.name + " " + layerInfo.id);
        listItem.layerId = layerInfo.id;
    }
    
    layerListBox.onChange = function(){
        var selectedLayerListItem = layerListBox.selection;
        if(selectedLayerListItem == undefined){ return; }
        
        var selectedLayerId = selectedLayerListItem.layerId;
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
const widgetType = ['image', 'button', 'text'];
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
const yAnchorType = ['top', 'center', 'bottom', 'stretch'];
function appendAnchorPanel(appendTarget){
    var anchorPanel = appendTarget.add("panel", undefined, "Anchor");

    for(var y = 0; y < yAnchorType.length; y++){
        var yAnchor = yAnchorType[y];
        
        var horizontalGroup = anchorPanel.add('group');
        for(var x = 0; x < xAnchorType.length; x++){
            var xAnchor = xAnchorType[x];
            
            var anchorButton = horizontalGroup.add("button", undefined, xAnchor + " - " + yAnchor);
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


function appendScrollRectPanel(appendTarget){
    var scrollRectPanel = appendTarget.add(
        "Panel{\
            text: 'ScrollRect',\
            orientation: 'row',\
            \
            horizontalCheckbox: Checkbox{ text: 'Horizontal' },\
            verticalCheckbox: Checkbox{ text: 'Vertical' },\
            \
            addScrollRectButton: Button{ text: 'Add Scroll Rect'}\
        }"
    );

    scrollRectPanel.addScrollRectButton.onClick = createScrollRectCallback(
        scrollRectPanel.horizontalCheckbox,
        scrollRectPanel.verticalCheckbox
    );
}

function createScrollRectCallback(horizontalCheckbox, verticalCheckbox){
    return function(){
        var selectedLayerListItem = mainWindow.layerGroup.layerList.selection;
        if(selectedLayerListItem == undefined){ return; }
        
        var selectedLayerId = selectedLayerListItem.layerId;
        
        setLayerConfig(config, selectedLayerId, 'hasScrollRect', true);
        setLayerConfig(config, selectedLayerId, 'isScrollRectHorizontal', horizontalCheckbox.value);
        setLayerConfig(config, selectedLayerId, 'isScrollRectVertical', verticalCheckbox.value);
        
        updateLayerStatusLabel(selectedLayerId);
    }
}


function appendGridPanel(appendTarget){
    var gridPanel = appendTarget.add(
        "Panel{\
                text: 'Grid',\
                orientation: 'row',\
                cellSizeXLabel: StaticText { text: 'Cell Size X' },\
                cellSizeXText: EditText { text: '100' },\
                \
                cellSizeYLabel: StaticText { text: 'Cell Size Y' },\
                cellSizeYText: EditText { text: '100' },\
                \
                spacingXLabel: StaticText { text: 'Spacing X' },\
                spacingXText: EditText { text: '0' },\
                \
                spacingYLabel: StaticText { text: 'Spacing Y' },\
                spacingYText: EditText { text: '0' },\
                \
                addGridButton: Button {text: 'Add Grid'}\
        }"
    );

    gridPanel.addGridButton.onClick = createGridCallback(
        gridPanel.cellSizeXText,
        gridPanel.cellSizeYText,
        gridPanel.spacingXText,
        gridPanel.spacingYText
    );
}

function createGridCallback(cellSizeXText, cellSizeYText, spacingXText, spacingYText){
    return function(){
        var selectedLayerListItem = mainWindow.layerGroup.layerList.selection;
        if(selectedLayerListItem == undefined){ return; }
        
        var selectedLayerId = selectedLayerListItem.layerId;
        
        setLayerConfig(config, selectedLayerId, 'hasGrid', true);
        setLayerConfig(config, selectedLayerId, 'gridCellSizeX', cellSizeXText.text);
        setLayerConfig(config, selectedLayerId, 'gridCellSizeY', cellSizeYText.text);
        setLayerConfig(config, selectedLayerId, 'gridSpacingX', spacingXText.text);
        setLayerConfig(config, selectedLayerId, 'gridSpacingY', spacingYText.text);
        
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
    optionGroup: Group{\
        orientation: 'column',\
        \
        currentSelectedLayerNameText: StaticText{\
            text: 'Select',\
            bounds: [0, 0, 400, 100]\
            properties: { multiline: true }\
        }\
    }\
}");


appendLayerList(mainWindow.layerGroup);

appendSkipPanel(mainWindow.optionGroup);
appendWidgetPanel(mainWindow.optionGroup);
appendAnchorPanel(mainWindow.optionGroup);
appendGridPanel(mainWindow.optionGroup);
appendScrollRectPanel(mainWindow.optionGroup);
appendSerializePanel(mainWindow.optionGroup);



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