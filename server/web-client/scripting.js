/*
  Copyright 2012 Rodja Trappe

  This file is part of cross copy.

  cross copy is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  cross copy is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with cross copy.  If not, see <http://www.gnu.org/licenses/>.
*/


/* jslint devel: true, browser: true, sloppy: true, eqeq: true, white: true, maxerr: 50, indent: 2 */

var internetExplorerSucks = 30;
var server = "/api";
var localHistory;
var secret;
var receiverRequest;
var watchRequest;
var listenerCount = 0;
var deviceId;

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

// guid generator from http://stackoverflow.com/questions/105034/how-to-create-a-guid-uuid-in-javascript
function guid() {
    var S4 = function() {
       return (((1+Math.random())*0x10000)|0).toString(16).substring(1);
    };
    return (S4()+S4()+"-"+S4()+"-"+S4()+"-"+S4()+"-"+S4()+S4()+S4());
}

function listen () {

  if (receiverRequest != undefined)
    receiverRequest.abort();

  if (secret === undefined || secret.length == 0)
    return;

  $('#step-2 h2').css({color: ''}); 
  
  if (uploader != undefined){
    uploader._options.action = "api/" + secret + "/";
    uploader._handler._options.action = uploader._options.action;
  }

  var latestKnownMessageId = "";
  if (localHistory[secret] && localHistory[secret].length > 0) 
    latestKnownMessageId = localHistory[secret][0].id;

  receiverRequest = $.ajax({
      url: server + '/' + secret + ".json?device_id=" + deviceId + "&since=" + latestKnownMessageId,
      cache: false,
      dataType: "json",
      success: function(res){
        trackEvent('succsess', 'GET');
        trackEvent('data', 'received');
        $.each(res, function(i, msg){
          paste(msg, "in");
        });
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
      cache: false,
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


function paste(msg, direction){
  msg.direction = direction;

  // convert relative file ref into hyperlink
  if (msg.data.indexOf('/api/' + secret) != -1)
    msg.data = '<a href="' + msg.data + '">' + msg.data.substring(('/api/' + secret).length + 1) + '</a>';
  
  var $li = $('<li>' + msg.data +'</li>\n');
  $li.autolink();
  $li.addClass(direction);
  if (msg.keep_for){
    var $countdown = $("<div class='countdown' title='seconds until message will be deleted from server'>" + msg.keep_for + "</div>");
    $li.prepend($countdown);
    $li.data("countdown_interval", setInterval(function(){
      var keptFor = parseInt($countdown.text());
      if (keptFor > 0) { $countdown.text(keptFor - 1); return }
      $countdown.fadeOut();
      clearInterval($li.data("cuntdown_interval"));
    }, 1000));
  }
  $('#current').prepend($li);
  $li.hide().slideDown();

  localHistory[secret].unshift(msg);
  storage && storage.setItem('localHistory', JSON.stringify(localHistory, null, 2));
}

function share(text){

  if (secret.length == 0){
    $('#step-2 h2').css({color: '#f00'});
    $('#step-1 h2').css({color: ''});
    return;
  }

  trackEvent('data', 'submitted');

  $.ajax({
      url: server + '/' + secret + ".json?device_id=" + deviceId,
      cache: false,
      type: 'PUT',
      processData: false,
      crossDomain: false,
      data: text,
      dataType: "json",
      success: function(response){
        trackEvent('succsess', 'GET');
        $('#step-1 h2').css({color: ''});       
        paste(response, "out");
        storage && storage.setItem('secrets', JSON.stringify([secret], null, 2));
      },
      error: function(xhr, status){
        $('#step-1 h2').css({color: '#f00'});
        trackEvent('error', 'PUT');
      },
      complete: function(xhr, status){
      }
  });
}

$(document).ready(function() {

  if (storage) deviceId = storage.getItem('device_id');
  if (deviceId == null) deviceId = guid();
  if (storage) storage.setItem('device_id', deviceId);

  if (storage)
    localHistory = JSON.parse(storage.getItem('localHistory'));
  if (localHistory == null) localHistory = {};

  if (storage){
    var secrets = JSON.parse(storage.getItem('secrets'));
    if (secrets != null && secrets.length > 0){
      $('#secret').val(secrets[0]); 
      onNewSecret();
    }
  }

  $('#secret').focus();
  $('#secret').select();

  $('#step-2 p').hide();

  $('#secret').keyup(function (e){
    if (secret != encodeURI($('#secret').val()))
      onNewSecret();
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

function onNewSecret(){
  secret = encodeURI($('#secret').val());
  showlocalHistory();
  listen();
  watch();
}

function showlocalHistory(){
  if (localHistory[secret] == null)
    localHistory[secret] = [];

  try{
    // using a copy of history so it won't get manipulated while fading...
    var oldPastes = JSON.parse(JSON.stringify(localHistory[secret]));
  } catch(e){
    var oldPastes = [];  
  }

  $('#history').fadeOut(function(){
    $(this).empty();
    $.each(oldPastes, function(i,e){
       if (e.data === undefined) return true; // continue
       var $li = $('<li class="' + (e.direction || 'in') + '">' + e.data +'</li>\n');
      $li.autolink();
      $('#history').append($li);
    });
    if (oldPastes.length > 0)
      $('#history').prepend($('<li class="new-session">locally stored history for this secret</li>'));
    $('#history').fadeIn();
  });
}

function showWhy(){
  $('#explenation a').fadeOut(200, function(){
    $('#why').slideDown(500);
  });
}

