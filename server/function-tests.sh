#!/bin/bash

TEST="../cmd-client/cross-copy -q -l"

report(){
  
  echo "$@ in line" `caller 1`
}

assertEqual(){
  [[ $1 == $2 ]] || report "$3 (expected '$1' but was '$2')"
}

##### FUNCTION TESTS

R=`$TEST some-secret "$DATA"`
assertEqual 0 $R "shoud have no direct deliverys"

DATA="the message"
( M=`$TEST some-secret`; assertEqual "$DATA" "$M" "should receive correct message" ) &
sleep 1
R=`$TEST some-secret "$DATA"`
assertEqual 1 $R "shoud have one direct delivery"



