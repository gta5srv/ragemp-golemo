//Событие отправки кастомных уведомлений игроку
//тип, расположение, сообщение, время
mp.events.add('notify', (type, layout, msg, time) => {
    if (global.loggedin) mp.gui.execute(`notify(${type},${layout},'${msg}',${time})`);
    else mp.events.call('authNotify', type, layout, msg, time)
});

