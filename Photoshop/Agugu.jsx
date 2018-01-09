#target photoshop

function getAllLayerNames(doc){
	var result = [];
	for (var i = 0; i < doc.layers.length; i++)  
	{
		result.push(doc.layers[i].name)
	}  
	return result;
}

function setLayerConfig(config, layerName, propertyName, propertyValue){
	if(config[layerName] == undefined){
		config[layerName] = {};
	}
	config[layerName][propertyName] = propertyValue;
}

// XMP
function loadConfigFromXMP(config, xmp){
	var configRoot = xmp.getProperty(aguguXmpNamespace, xmpConfigRootTag);
	if(configRoot == undefined){
		return;
	}

	var layersRootPath = XMPUtils.composeStructFieldPath(aguguXmpNamespace, xmpConfigRootTag, 
														 aguguXmpNamespace, xmpLayersRootTag);
	var layersRoot = xmp.getProperty(aguguXmpNamespace, layersRootPath);
	if(layersRoot == undefined){
		return;
	}
    
	var layerCount = xmp.countArrayItems(aguguXmpNamespace, layersRootPath);
	for(var i = 1; i <= layerCount; i++){
		var layerPath = XMPUtils.composeArrayItemPath(aguguXmpNamespace, layersRootPath, i);
		var layerNamePath = XMPUtils.composeStructFieldPath(aguguXmpNamespace, layerPath,
															aguguXmpNamespace, xmpLayerNameTag);
		var layerName = xmp.getProperty(aguguXmpNamespace, layerNamePath).value;
        
		var iter = xmp.iterator(XMPConst.ITERATOR_JUST_CHILDREN , aguguXmpNamespace, layerPath);
		var layerProperty = iter.next();
		while( layerProperty != null){
			var propertyPath = layerProperty.path;
            var propertyName = getLeafPropertyName(propertyPath, aguguNamespacePrefix);
			var propertyValue = xmp.getProperty(aguguXmpNamespace, propertyPath).value;
			
			setLayerConfig(config, layerName, propertyName, propertyValue);
			layerProperty = iter.next();
		}
	}
} 

function getLeafPropertyName(xmpPath, namespacePrefix){
	var pathElements = xmpPath.split("/");
	var pathElementsCount = pathElements.length;
	var leafPropertyNameWithNamespace = pathElements[pathElementsCount-1];
	return leafPropertyNameWithNamespace.replace(namespacePrefix, "")
}

// Widget Type
const widgetType = ['image', 'button', 'text']
function createWidgetButtonCallback(widgetType){
	return function(){
		var selectedLayerListItem = mainWindow.layerGroup.layerList.selection;
		if(selectedLayerListItem == undefined){
			return;
		}
		var selectedLayerName = selectedLayerListItem.text;
		
		setLayerConfig(config, selectedLayerName, 'widgetType', widgetType);
		updateLayerStatusLabel(selectedLayerName);
	}
}

// Anchor Type
const xAnchorType = ['left', 'center', 'right', 'stretch']
const yAnchorType = ['top', 'center', 'bottom', 'stretch']
function createAnchorButtonCallback(xAnchor, yAnchor){
	return function(){
		var selectedLayerListItem = mainWindow.layerGroup.layerList.selection;
		if(selectedLayerListItem == undefined){
			return;
		}
		var selectedLayerName = selectedLayerListItem.text;
		
		setLayerConfig(config, selectedLayerName, 'xAnchor', xAnchor);
		setLayerConfig(config, selectedLayerName, 'yAnchor', yAnchor);
		
		updateLayerStatusLabel(selectedLayerName);
	}
}

function updateLayerStatusLabel(selectedLayerName){
	mainWindow.optionGroup.currentSelectedLayerNameText.text = getLayerStatusText(selectedLayerName);
}

function getLayerStatusText(selectedLayerName) {
	var showText = selectedLayerName;
	if(config[selectedLayerName] != undefined &&
	   config[selectedLayerName].length != 0){
		for(var property in config[selectedLayerName]){
			showText += "\n";
			showText += (property + " : " + config[selectedLayerName][property]);
		}
	}
	return showText;
}

var config = {};

const aguguXmpNamespace = "http://www.agugu.org/";
const aguguNamespacePrefix = "agugu:";
const xmpConfigRootTag = "Config";
const xmpLayersRootTag = "Layers";
const xmpLayerNameTag = "Name";

// Load XMPMeta reference
if(ExternalObject.AdobeXMPScript == undefined) {
	ExternalObject.AdobeXMPScript = new ExternalObject('lib:AdobeXMPScript');
}

var xmp = new XMPMeta(activeDocument.xmpMetadata.rawData);
XMPMeta.registerNamespace(aguguXmpNamespace, aguguNamespacePrefix);

loadConfigFromXMP(config, xmp);

var mainWindow = new Window ("palette", "Agugu");
mainWindow.orientation = "row";

mainWindow.frameLocation = [400,160];
mainWindow.layerGroup = mainWindow.add("group");
mainWindow.layerGroup.layerList = mainWindow.layerGroup.add ("listbox", [0, 0, 150, 250]);

var allLayerNames = getAllLayerNames(activeDocument);
for(layerName in allLayerNames)
{
	mainWindow.layerGroup.layerList.add ("item", allLayerNames[layerName]);
}
mainWindow.layerGroup.layerList.onChange = function(){
	var selectedLayerListItem = mainWindow.layerGroup.layerList.selection;
	if(selectedLayerListItem == undefined){
		return;
	}
	var selectedLayerName = selectedLayerListItem.text;
	updateLayerStatusLabel(selectedLayerName);
}



mainWindow.optionGroup = mainWindow.add("group");
mainWindow.optionGroup.orientation = "column";
mainWindow.optionGroup.currentSelectedLayerNameText = 
	mainWindow.optionGroup.add ('statictext',  [0, 0, 400, 100], 'Select', {multiline: true});

	
	
mainWindow.optionGroup.widgetTypePanel = mainWindow.optionGroup.add("panel", undefined, "UI Type");
mainWindow.optionGroup.widgetTypePanel.orientation = "row";

for(var w = 0; w< widgetType.length; w++){
	var widget = widgetType[w];
	var widgetButton = mainWindow.optionGroup.widgetTypePanel.add("button", undefined, widget);
	widgetButton.onClick = createWidgetButtonCallback(widget);
}



mainWindow.optionGroup.anchorPanel = mainWindow.optionGroup.add("panel", undefined, "Anchor");

for(var y = 0; y < yAnchorType.length; y++){
    var yAnchor = yAnchorType[y];
    
    var horizontalGroup = mainWindow.optionGroup.anchorPanel.add('group');
    for(var x = 0; x < xAnchorType.length; x++){
        var xAnchor = xAnchorType[x];
        
        var anchorButton = horizontalGroup.add("button", undefined, xAnchor + " - " + yAnchor);
        anchorButton.onClick =createAnchorButtonCallback(xAnchor, yAnchor)
     }
}



mainWindow.show();

while(true){
	app.refresh();
}