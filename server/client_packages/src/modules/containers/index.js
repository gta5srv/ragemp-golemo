mp.events.add("openContainerMenu", (json) => {
  if (!loggedin || chatActive || editing || cuffed) return;
  global.containerMenu = mp.browsers.new('package://browser/ContainerSystem/index.html');
  global.containerMenu.active = true;
  global.menuOpen();
  var data = JSON.stringify(json);
  global.containerMenu.execute(`containerMenu.setinfo(${data})`);
});

mp.events.add("closeContainer", () => {
  if(global.containerMenu)
  {
    global.menuClose();
    global.containerMenu.active = false;
    global.containerMenu.destroy();
  }
});

mp.events.add("openContainer", () => {
  mp.events.callRemote('openContainer');
});
