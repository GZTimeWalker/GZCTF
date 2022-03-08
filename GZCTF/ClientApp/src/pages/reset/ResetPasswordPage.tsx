import React, { FC, FormEvent, useEffect, useState } from 'react';
import { useQueryParams } from '../../common/hooks/use-query-params';
import { Redirect } from 'react-router';
import { USER_API } from '../../redux/user.api';
import {
  Button,
  Container,
  Flex,
  FormControl,
  FormErrorMessage,
  FormLabel,
  Heading,
  Input,
  useToast
} from '@chakra-ui/react';
import { resolveMessage } from '../../common/utils';

export const ResetPasswordPage: FC = () => {
  const { data } = USER_API.useStatusQuery();
  const [reset, { isLoading, error, isSuccess }] = USER_API.useResetPasswordMutation();
  const { email, token } = useQueryParams();
  const toast = useToast();
  const [password, setPassword] = useState('');

  const onSubmit = React.useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (password) {
        reset({ password, email: email!, rToken: token! });
      }
    },
    [password, email, token, reset]
  );

  useEffect(() => {
    if (isSuccess) {
      toast({
        title: '重置密码成功',
        description: '即将跳转到登陆页',
        status: 'success',
        duration: 5000
      });
      setTimeout(() => {
        window.location.href = '/login';
      }, 3000);
    }
  }, [isSuccess, toast]);

  if (data || !email || !token) {
    return <Redirect to="/" />;
  }

  return (
    <Container p="32px" maxWidth="40ch">
      <Heading mb="32px" size="lg">
        重置密码
      </Heading>
      <form onSubmit={onSubmit}>
        <FormControl id="email" my="12px" isRequired>
          <FormLabel>邮箱</FormLabel>
          <Input type="email" value={email} disabled />
        </FormControl>
        <FormControl id="password" my="12px" isRequired isInvalid={!!error}>
          <FormLabel>密码</FormLabel>
          <Input
            autoComplete="no"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
          {error && <FormErrorMessage>{resolveMessage(error)}</FormErrorMessage>}
        </FormControl>
        <Flex mt="32px" direction="row-reverse">
          <Button
            type="submit"
            isLoading={isLoading}
            disabled={!password || isLoading}
          >
            提交
          </Button>
        </Flex>
      </form>
    </Container>
  );
};
