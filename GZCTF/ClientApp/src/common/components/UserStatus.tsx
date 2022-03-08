import { Text, Box, Button, Heading, HStack, VStack } from '@chakra-ui/react';
import { Link } from 'react-router-dom';
import React, { FC } from 'react';
import { Redirect } from 'react-router';
import { USER_API } from 'src/redux/user.api';

export const UserStatus: FC = () => {
  const { data: user } = USER_API.useStatusQuery();
  const [logout, { isLoading: isLoggingOut, isSuccess: isLogOutSuccess }] =
    USER_API.useLogoutMutation();

  if (isLogOutSuccess || !user) {
    return <Redirect to="/login" />;
  }

  return (
    <Box py="6px" px="12px">
      <VStack spacing="12px">
        <Heading size="md">{user.name}</Heading>
        <Text>{user.email}</Text>
        <HStack>
          <Link to="/account-manage">
            <Button size="sm" variant="ghost">
              账户管理
            </Button>
          </Link>
          <Link to="/change-info">
            <Button size="sm" variant="ghost">
              修改资料
            </Button>
          </Link>
          <Button
            size="sm"
            variant="ghost"
            disabled={isLoggingOut}
            isLoading={isLoggingOut}
            onClick={() => logout()}
          >
            注销
          </Button>
        </HStack>
      </VStack>
    </Box>
  );
};
