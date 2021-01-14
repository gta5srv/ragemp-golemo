function set(data) {
	data = JSON.parse(data);
	$('#spawn-select').css('display', 'flex');
	
    if (data[2] !== true) {
		$('#home-spawn').addClass('disabled');
		$('#home-spawn button').prop("disabled", true);
		$('#home-spawn .title-text').append('<span>Недоступно</span>');
	}
	
    if (data[1] !== true){
		$('#org-spawn').addClass('disabled');
		$('#org-spawn button').prop("disabled", true);
		$('#org-spawn .title-text').append('<span>Недоступно</span>');
    } 
}

function spawn(id) {
    mp.trigger('spawn', id);
}