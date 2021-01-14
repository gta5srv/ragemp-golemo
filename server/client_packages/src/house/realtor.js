const localPlayer = mp.players.local;

mp.events.add("openRealtorMenu", () => {
	if(!global.loggedin) return;
	global.menuOpen();
	global.realtorMenu = mp.browsers.new('package://browser/modules/RealtorManager/index.html');
	global.realtorMenu.active = true;
	global.realtorMenu.execute(`realtorMenu.active=true`);
});

mp.events.add("closeRealtorMenu", () => {
	if(global.realtorMenu){
		global.menuClose();
		global.realtorMenu.active = false;
		global.realtorMenu.destroy();
	}
});

mp.events.add("LoadHouse", (houses) => {
	global.realtorMenu.execute(`realtorMenu.houses=${houses}`);
});

mp.events.add("SelectHouseClass", (hclass) => {
	mp.events.callRemote("LoadHouseToMenu", hclass);
});

mp.events.add("buyInfoHome", (hclass, x, y) => {
	mp.events.callRemote("buyRealtorInfoHome", hclass, x, y);
});

mp.events.add("getStreetAndAreaHouse", (x, y, z) => {
	var street = mp.game.pathfind.getStreetNameAtCoord(x, y, z, 0, 0);
    let areahouse  = mp.game.zone.getNameOfZone(x, y, z);
	
	global.realtorMenu.execute(`realtorMenu.street='${mp.game.ui.getStreetNameFromHashKey(street.streetName)}'`);
    global.realtorMenu.execute(`realtorMenu.crossingRoad='${mp.game.ui.getLabelText(areahouse)}'`);
});