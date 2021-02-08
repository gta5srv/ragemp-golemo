mp.events.add('PLAYER::TASK_LEAVE_ON_VEHICLE', () => {
    let vehicle = localplayer.vehicle;
    if(vehicle != null) {
        localplayer.taskLeaveVehicle(vehicle.handle, 1);
    }
    //todo проверить
    if(cuffed){
		while(localplayer.vehicle){
			mp.game.wait(100);
		}
		mp.events.callRemote("cuffUpdate");
	}
});

//vehicle.handle
mp.events.add('PLAYER::TASK_ENTER_IN_VEHICLE', (vehicle, timeout, seat, speed, mode) =>{
    if(vehicle != null || !localplayer.getVariable("IS_DYING")) {
        localplayer.taskEnterVehicle(vehicle.handle, timeout, seat, speed, mode, 0);
    }
});