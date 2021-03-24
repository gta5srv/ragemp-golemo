const order = (type, name, level, count, max) => ({type, name, level, count, max});
const orders = [
  order(0, "Инструменты", 1, 5, 4),   //Tools
  order(1, "Топливо", 2, 2, 5),       //Fuel
  order(2, "Транспорт", 4, 1, 1),     //Vehicle
  order(6, "Боеприпасы", 3, 2, 10),    //Weapons
  order(7, "Ткань", 1, 5, 20),         //Cloth
  order(8, "Продукты", 1, 7, 10),      //Food
  order(9, "Краска", 1, 10, 30),        //TattooPaint
  order(12, "Запчасти", 2, 3, 20),     //SpareParts
]

var truckJobMenu = new Vue({
  el: "#app",
  data: {
    active: true,
    header: ["Работа Дальнобойщика", "Выберите заказ"],
    page: 0,
    trucks: {"1":"boxville4", "2":"mule", "3":"benson", "4":"pounder"},
    truck : null,
    orders: orders,
    playerLevel: 4,
    getTruckAccessLevel: function () {
      return Object.keys(this.trucks).find(x => this.trucks[x] == this.truck);
    },
    generateNumber: function (min, max) {
      return Math.floor(Math.random()*(max-min+1)+min);
    }
  },
  methods: {
    selectTruck(truck) {
      this.truck = truck;
      if(this.playerLevel < this.getTruckAccessLevel()) return;
      this.page  = 1;
    },
    takeOrder(orderType) {
      mp.trigger("JOBS::TRUCKER_TAKE_ORDER", orderType, this.truck);
      this.page = 0;
      this.closePanel();
    },
    setInfo(json) {
      this.trucks = json[0];
      this.playerLevel = json[1];
      this.active = true;
    },
    reset() {
      this.active = false;
    },
    closePanel() {
      if(this.page != 0) {
        this.page = 0;
        return;
      }
      this.reset();
      mp.trigger("JOBS::TRUCKER_CLOSE_MENU");
    },
  }
})
