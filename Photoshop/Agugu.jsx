var mainWindow = new Window ("dialog", "Agugu");

mainWindow.frameLocation = [400,160];
mainWindow.size = [200, 200];
mainWindow.btnPnl = mainWindow.add("panel", [10,10,180,180], "Export");
mainWindow.btnPnl.add("button", [10,10,80,80], "OK");
mainWindow.show();