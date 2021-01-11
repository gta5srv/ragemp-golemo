global.reportsPanel = mp.browsers.new('package://browser/ReportPanel/index.html');
global.reportsPanel.active = true;

mp.keys.bind(0x23, false, function () {
    if (!loggedin || chatActive || editing || new Date().getTime() - lastCheck < 1000 || global.menuOpened) return;
	global.menuOpen();
	global.reportsPanel.execute(`areportpanel.active=true`);
	lastCheck = new Date().getTime();
});

mp.events.add("reportAnswer", (json) => {
	global.reportsPanel.execute(`areportpanel.loadreply(${json})`);
});

mp.events.add("closeReportPanel", () => {
  global.menuClose();
});