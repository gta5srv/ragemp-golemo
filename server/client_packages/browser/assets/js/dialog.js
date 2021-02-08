var dialog = new Vue({
    el: '.dialog',
    data: {
        active: false,
        title: "",
    },
    methods: {
        yes: function () {
            mp.trigger('dialogCallback', true);
        },
        no: function () {
            mp.trigger('dialogCallback', false);
        }
    }
})