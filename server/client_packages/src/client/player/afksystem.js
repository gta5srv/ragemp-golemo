global.afkSecondsCount = 0;

setInterval(function () {
    if (!global.menuOpened) {

        afkSecondsCount++;
        if (afkSecondsCount >= 900) {
			if(localplayer.getVariable('IS_ADMIN') == true) afkSecondsCount = 0;
			else {
				mp.gui.chat.push('Вы были исключены с сервера за афк. Сделайте переподключение.');
				mp.events.callRemote('AFK::KICK_PLAYER');
			}
        }
    }
}, 1000);