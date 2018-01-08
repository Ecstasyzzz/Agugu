#target photoshop


function getAllLayerNames(doc){
	var result = [];
	for (var i = 0; i < doc.layers.length; i++)  
	{
		result.push(doc.layers[i].name)
	}  
	return result;
}


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

mainWindow.optionGroup = mainWindow.add("group");
mainWindow.optionGroup.buttonPanel = mainWindow.optionGroup.add("panel", undefined, "Export");
mainWindow.optionGroup.buttonPanel.okButton = mainWindow.optionGroup.buttonPanel.add("button", undefined, "OK");

mainWindow.show();

while(true){
	app.refresh();
}