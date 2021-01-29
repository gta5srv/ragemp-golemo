mp.events.add('PLAYER::TASK_LEAVE_ON_VEHICLE', () => {
    if(localplayer.vehicle != null) {
        let vehicle = localplayer.vehicle;
        localplayer.taskLeaveVehicle(vehicle.handle, 1);
    }
});