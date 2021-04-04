let rentMenu = null;
let rentCarBlip;
mp.events.add({
	"RENT::OPEN_RENT_MENU": (json) => {
		if (!loggedin || chatActive || editing || cuffed) return;
		rentMenu = mp.browsers.new('package://browser/modules/RentCars/index.html');
		rentMenu.active = true;
		global.menuOpen();
		rentMenu.execute(`rentcars.openMenu(${json})`);
	},
	"RENT::CLOSE_RENT_MENU": () => {
		if(rentMenu)
		{
			global.menuClose();
			rentMenu.active = false;
            rentMenu.destroy();
            rentMenu = null;
		}
	},
	"CLIENT:::RENT::BUY_RENT_CAR": (vehicle, time) => {
        mp.events.callRemote("SERVER:::RENT::BUY_RENT_CAR", vehicle, time);
	},
	"CLIENT:::RENT::GET_VEHICLE_INFORMATION": (vehicle) => {
        var vhash = mp.game.gameplay.getHashKey(vehicle);
		let maxSpeed = (mp.game.vehicle.getVehicleModelMaxSpeed(vhash) * 3.6).toFixed(0);
        let maxPassenger = mp.game.vehicle.getVehicleModelMaxNumberOfPassengers(vhash);
        rentMenu.execute(`rentcars.loadVehicleInfo(${maxSpeed},${maxPassenger})`);
	},
	"RENT::CAR_CREATE_BLIP": async (vehicle, sprite) => {
		if(rentCarBlip) {
			await mp.events.call("RENT::RENT_CAR_BLIP_DELETE");
		}
		if(vehicle && mp.vehicles.exists(vehicle)) {
			rentCarBlip = mp.blips.new(sprite, vehicle.position,
            {
                name: 'Арендованный транспорт',
                scale: 1,
                color: 5,
                dimension: 0,
            });
		}
	},
	"RENT::RENT_CAR_BLIP_DELETE": async () => {
		if(rentCarBlip) {
			await rentCarBlip.destroy();
			rentCarBlip = null;
		}
	}
});