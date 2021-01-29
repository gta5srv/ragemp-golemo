mp.events.add('PLAYER::TASK_LEAVE_ON_VEHICLE', () => {
    if(localplayer.vehicle != null) {
        let vehicle = localplayer.vehicle;
        localplayer.taskLeaveVehicle(vehicle.handle, 1);
    }
});

//vehicle.handle
mp.events.add('PLAYER::TASK_ENTER_IN_VEHICLE', (vehicle, timeout, seat, speed, mode) =>{
    if(vehicle != null || !localplayer.getVariable("IS_DYING")) {
        localplayer.taskEnterVehicle(vehicle.handle, timeout, seat, speed, mode, 0);
    }
});