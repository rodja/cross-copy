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


R=`$TEST -k 2 $SECRET "1"`
R=`$TEST -k 1 $SECRET "2"`
R=`$TEST -r $SECRET | grep -Po '"data":.*?[^\\\\]",'`
assertEqual '"data":"1",
"data":"2",' "$R" "should get both messages"
sleep 1
R=`$TEST -r $SECRET | grep -Po '"data":.*?[^\\\\]",'`
assertEqual '"data":"1",' "$R" "second message should have been kept for only a second"
sleep 1
R=`$TEST -r $SECRET | grep -Po '"data":.*?[^\\\\]",'`
assertEqual '' "$R" "first message should have been kept for only two seconds"
SECRET=`uuidgen`


wait
