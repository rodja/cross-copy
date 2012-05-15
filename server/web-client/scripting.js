/*jslint devel: true, browser: true, sloppy: true, eqeq: true, white: true, maxerr: 50, indent: 2 */

var internetExplorerSucks = 30;
var server = "/api";
var localHistory = undefined;
var secret;
var receiverRequest;
var watchRequest;
var listenerCount = 0;




// Feature detect + local reference (from http://mathiasbynens.be/notes/localstorage-pattern)
var storage = (function () {
  "use strict";
  var uid = new Date(),
    storage,
    result;
  try {
    (storage = window.localStorage).setItem(uid, uid);
    result = storage.getItem(uid) == uid;
    storage.removeItem(uid);
    return result && storage;
  } catch (e) {}
}() );

function listen () {

  secret = encodeURI($('#secret').val());

  if (receiverRequest != undefined)
    receiverRequest.abort();

  if (secret === undefined || secret.length == 0)
    return;

  $('#step-2 h2').css({color: ''}); 
  
  if (uploader != undefined){
    uploader._options.action = "api/" + secret + "/";
    uploader._handler._options.action = uploader._options.action;
  }

  receiverRequest = $.ajax({
      url: server + '/' + secret,
      success: function(response){
        trackEvent('succsess', 'GET');
        trackEvent('data', 'received');
        paste(response, "in");
      },
      error: function(xhr, status){
        trackEvent('error', 'GET');
	    },
      complete: function(xhr, status){
        if (status != "abort"){
          setTimeout(listen, 50);
        }
      }
  });
}

var numbersAsWords = ['no','one','two','three','four', 'five','six','seven','eight','nine', 'ten','eleven','twelve'];

function watch(){
  if (watchRequest != undefined)
    watchRequest.abort();

  watchRequest = $.ajax({
      url: server + '/' + secret + '?watch=listeners&count=' + (listenerCount + 1),
      success: function(response){
        trackEvent('watch_listeners', 'changed');
        listenerCount = response - 1;

        // wait for reconnecting myself
        if (listenerCount == -1){
          return;
        }

        var msg = "(" + (listenerCount < 13 ? numbersAsWords[listenerCount] : listenerCount) + " other device" + (listenerCount > 1 ? "s use" : " uses") + " this secret)";
        if ($('#step-2 p').is(":visible"))
          $('#step-2 p').text(msg);
        else
          $('#step-2 p').text(msg).hide().slideDown(400);    
      },
      error: function(xhr, status){
        trackEvent('error', 'GET listeners count');
	    },
      complete: function(xhr, status){
        if (status != "abort"){
          setTimeout(watch, 50);
        }
      }
  });
}


function paste(data, direction){

  if (data.indexOf('/api/' + secret) != -1)
    data = '<a href="' + data + '">' + data.substring(('/api/' + secret).length + 1) + '</a>';
  
  var $li = $('<li>' + data +'</li>\n');
  $li.addClass(direction);
  $('#received').prepend($li);
  $li.hide().slideDown();

  var pastes = [];
  $("ul li").each(function() { 
    var direction = $(this).attr('class');    
    if (direction === 'in' || direction === 'out')   
      pastes.push( {'direction': direction, 'msg' : $(this).text() });
  });
  localHistory[secret] = pastes;
  storage && storage.setItem('localHistory', JSON.stringify(localHistory, null, 2));
}

function share(text){

  if (secret.length == 0){
    $('#step-2 h2').css({color: '#f00'});
    $('#step-1 h2').css({color: ''});
    return;
  }

  if (receiverRequest != undefined)
    receiverRequest.abort();

  trackEvent('data', 'submitted');

  $.ajax({
      url: server + '/' + secret,
      type: 'PUT',
      processData: false,
      crossDomain: false,
      data: text,
      dataType: "text",
      success: function(response){
        if (response == 0){
          $('#step-1 h2').css({color: '#f00'});
          trackEvent('error', 'lonley PUT');
        }
        else {
          trackEvent('succsess', 'GET');
          $('#step-1 h2').css({color: ''});       
          paste(text, "out");
          storage && storage.setItem('secrets', JSON.stringify([secret], null, 2));
        }
      },
      error: function(xhr, status){
        $('#step-1 h2').css({color: '#f00'});
        trackEvent('error', 'PUT');
      },
      complete: function(xhr, status){
receiverRequest = undefined;
        setTimeout(listen, 50);
      }
  });
}

$(document).ready(function() {

  try{
    if (storage)
      localHistory = JSON.parse(storage.getItem('localHistory'));
    if (localHistory == null) localHistory = {};

    if (storage){
      var secrets = JSON.parse(storage.getItem('secrets'));
      if (secrets != null && secrets.length > 0){
        $('#secret').val(secrets[0]); 
        listen();
        watch();
        showlocalHistory();
      }
    }
  } catch (e){ localHistory = {}; }
 
  $('#secret').focus();
  $('#secret').select();

  $('#step-2 p').hide();

  $('#secret').keyup(function (e){
    if (secret == encodeURI($('#secret').val()))
      return;

    listen();

    watch();

    showlocalHistory();
  });

  $('#data').keyup(function (e){
    
    if (e.which != 13)
      return;
    share($('#data').val());    
  });
  $('#data').keypress(function(e){
    if (e.which == 13)
      e.preventDefault();
  });
  $('#data').autosize();
  
});

function showlocalHistory(){

  var oldPastes = localHistory[secret];
  if (oldPastes == null)
    oldPastes = [];

  $('#received').fadeOut(function(){
    $(this).empty();
    $.each(oldPastes, function(i,e){
       var d = e.direction ? e.direction : 'in';
       var $li = $('<li class="' + d + '">' + (e.msg != undefined ? e.msg : e) +'</li>\n');
      $('#received').append($li);
    });
    if (oldPastes.length > 0)
      $('#received').prepend($('<li class="new-session">locally stored localHistory for this secret</li>'));
    $('#received').fadeIn();
  });
}

function showWhy(){
  $('#explenation a').fadeOut(200, function(){
    $('#why').slideDown(500);
  });
}

