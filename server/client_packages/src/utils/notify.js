//Событие отправки кастомных уведомлений игроку
//тип, расположение, сообщение, время
mp.events.add('notify', (type, layout, msg, time) => {
    if (global.loggedin) mp.gui.execute(`notify(${type},${layout},'${msg}',${time})`);
    else mp.events.call('authNotify', type, layout, msg, time)
});

global.INTERACTIONCHECK = false;
mp.events.add('playerInteractionCheck', function (state){
    global.INTERACTIONCHECK = state;
});
mp.events.add('render', () => {
    if (global.INTERACTIONCHECK === true && !global.menuOpened) {
        mp.game.ui.resetHudComponentValues(10);
        mp.game.ui.setHudComponentPosition(10, 0, 0); //меняйте эти два нуля. Точно не помню, но первое это по вертикали, второе по горизонтали. от 0 до 1 пробуйте перемещать
        mp.game.ui.setTextComponentFormat('STRING');
        mp.game.ui.addTextComponentSubstringPlayerName("~h~Нажмите ~g~ ~INPUT_CONTEXT~ ~s~ для взаимодействия");
        mp.game.ui.displayHelpTextFromStringLabel((0), false, true, -1);
    }
});