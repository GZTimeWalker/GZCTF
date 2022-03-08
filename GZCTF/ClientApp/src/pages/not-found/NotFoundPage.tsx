import { Center, Heading, VStack } from '@chakra-ui/react';
import React, { FC } from 'react';

export const NotFoundPage: FC = () => {
  return (
    <Center minHeight="100vh">
      <VStack>
        <Heading size="2xl">Oh，看起来你迷路了</Heading>
        <Heading size="lg" pt="1em">但是这里可不是你该来的地方</Heading>
      </VStack>
    </Center>
  );
};
