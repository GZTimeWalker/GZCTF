import React, { FC } from 'react';
import { VStack, Center, Spinner, Text } from '@chakra-ui/react';
import { resolveMessage } from '../utils';
import { FetchBaseQueryError } from '@reduxjs/toolkit/dist/query';
import { SerializedError } from '@reduxjs/toolkit';

export interface LoadingMaskProps {
  error?: FetchBaseQueryError | SerializedError;
  message?: string;
}

export const LoadingMask: FC<LoadingMaskProps> = ({ error, message }) => {
  return (
    <Center w="100%" h="100%">
      <VStack spacing={4}>
        <Spinner thickness="4px" size="xl" color="brand.100" />
        {!error && message && <Text>{message}</Text>}
        {error && <Text>{resolveMessage(error)}</Text>}
      </VStack>
    </Center>
  );
};
