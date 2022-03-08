import React, { FC, useEffect } from 'react';
import { USER_API } from '../../redux/user.api';
import { Redirect } from 'react-router';
import { useQueryParams } from '../../common/hooks/use-query-params';
import { LoadingMask } from '../../common/components/LoadingMask';
import { Box, VStack, Heading, Center } from '@chakra-ui/react';

export const EmailConfirmPage: FC = () => {
  const [confirm, { isLoading, error, isSuccess }] = USER_API.useConfirmChangingEmailMutation();
  const { token, email } = useQueryParams();

  useEffect(() => {
    if (token && email) {
      confirm({
        token,
        email
      });
    }
  }, [token, email, confirm]);

  useEffect(() => {
    if (isSuccess) {
      setTimeout(() => {
        window.location.href = '/login';
      }, 3000);
    }
  }, [isSuccess]);

  if (!token || !email) {
    return <Redirect to="/" />;
  }

  if (isLoading) {
    return (
      <Box h="100vh" w="100vw">
        <LoadingMask message="正在验证邮箱" />
      </Box>
    );
  }

  if (error) {
    return (
      <Box h="100vh" w="100vw">
        <LoadingMask error={error} />
      </Box>
    );
  }

  return (
    <Center h="100vh" w="100vw">
      <VStack>
        <Heading size="2xl">验证成功</Heading>
        <Heading size="lg" pt="1em">
          即将跳转至登陆页
        </Heading>
      </VStack>
    </Center>
  );
};
