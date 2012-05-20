#!/bin/bash

TEST="../cmd-client/cross-copy -q -l"

report(){
  echo $@
}

assertEqual(){
  [[ $1 == $2 ]] || report "$3 (expected '$2' but was '$1')"
}

DATA="the message"
( M=`$TEST some-secret`; assertEqual "$DATA" "$M" "should receive correct message" ) &
sleep 1
R=`$TEST some-secret "$DATA"`
assertEqual 1 $R "shoud have one direct delivery"
