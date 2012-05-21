#!/bin/bash

TEST="../cmd-client/cross-copy -q -l"

report(){
  
  echo "$@ in line" `caller 1`
}

assertEqual(){
  [[ $1 == $2 ]] || report "$3 (expected '$1' but was '$2')"
}

##### FUNCTION TESTS

DATA="the message"
SECRET=`uuidgen`

( M=`$TEST $SECRET`; assertEqual "$DATA" "$M" "should receive correct message" ) &
sleep 1
R=`$TEST $SECRET "$DATA"`
assertEqual 1 $R "shoud have one direct delivery"
SECRET=`uuidgen`


R=`$TEST $SECRET "$DATA"`
assertEqual 0 $R "shoud have no direct deliverys"
R=`$TEST -r $SECRET | grep -Po '"data":.*?[^\\\\]",'`
assertEqual '"data":"the message",' "$R" "should get recently stored data"
SECRET=`uuidgen`

wait
