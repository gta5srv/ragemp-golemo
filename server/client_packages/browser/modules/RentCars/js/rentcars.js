var rentcars = new Vue({
  el: "#app",
  data: {
    active: true,
    header: null,
    vehicle: null,
    vehicles: {},
    rentMinutes: 30,
    isModalOpen: false,
    money: 0,
    maxSpeed: 0,
    maxPassengers: 0,
  },
  methods: {
    openMenu(json) {
      this.active = true;
      this.header = json.header;
      this.money = json.money;
      this.vehicles = json.vehicles;
    },
    selectModel(vehicle) {
      this.vehicle = vehicle;
      this.isModalOpen = true;
      mp.trigger("CLIENT:::RENT::GET_VEHICLE_INFORMATION", vehicle);
    },
    //Подгрузка данных машины
    loadVehicleInfo(speed, number) {
      this.maxSpeed = speed;
      this.maxPassengers = number;
    },
    changeRentTimeMinutes(action) {
      if(action == "add") {
        this.rentMinutes + 10 > 180 ? 0 : this.rentMinutes += 10
      } else {
        this.rentMinutes - 10 < 10 ? 10 : this.rentMinutes -= 10
      }
    },
    rentVehicle: function() {
      if(this.needMoney) return;
      if(this.vehicle != null) {
        mp.trigger("CLIENT:::RENT::BUY_RENT_CAR", this.vehicle, this.rentMinutes);
      }
    },
    modalClose() {
      this.isModalOpen = false;
      this.vehicle = null;
      this.rentMinutes = 30;
      this.maxSpeed = 0;
      this.maxPassengers = 0;
    },
    reset() {
      this.modalClose();
      this.active = false;
    },
    closePanel: function() {
      this.reset();
      mp.trigger("RENT::CLOSE_RENT_MENU");
    },
  },
  computed: {
    needMoney: function() {
      if(this.money >= (this.vehicles[this.vehicle] * (this.rentMinutes/10))) return false;
      else return true;
    },
  }
})
