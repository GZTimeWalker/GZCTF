import {
  Alert,
  AlertIcon,
  Box,
  Button,
  Center,
  Flex,
  FormControl,
  FormErrorMessage,
  FormLabel,
  Heading,
  Input,
  Spacer,
  useBoolean,
  useDisclosure,
  useToast,
  VStack
} from '@chakra-ui/react';
import { or, resolveMessageForRateLimit } from '../../common/utils';
import React, { FC, FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import { LogoIcon } from '../../common/components/LogoIcon';
import { useQueryParams } from '../../common/hooks/use-query-params';
import { UserLoginDto, UserRegisterDto, USER_API } from 'src/redux/user.api';
import { ForgetPasswordModal } from '../../common/components/ForgetPasswordModal';

export const LoginPage: FC = () => {
  const [isToLogin, { toggle: toggleIsToLogin }] = useBoolean(true);
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { redirect } = useQueryParams();
  const toast = useToast();

  const [login, { isLoading: isLoggingIn, isSuccess: isLogInSuccess, error: loginError }] =
    USER_API.useLoginMutation();
  const [loginDto, setLoginDto] = useState<UserLoginDto>({
    userName: '',
    password: ''
  });
  const isLoginDisabled = useMemo(() => Object.values(loginDto).some(v => !v), [loginDto]);

  const [
    register,
    { isLoading: isRegistering, isSuccess: isRegisterSuccess, error: registerError }
  ] = USER_API.useRegisterMutation();
  const [registerDto, setRegisterDto] = useState<UserRegisterDto>({
    userName: '',
    password: '',
    email: ''
  });
  const isRegisterDisabled = useMemo(
    () => Object.values(registerDto).some((v) => !v),
    [registerDto]
  );

  const onLogin = React.useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!isLoginDisabled) {
        login(loginDto);
      }
    },
    [isLoginDisabled, loginDto, login]
  );

  const onRegister = React.useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!isRegisterDisabled) {
        register(registerDto);
      }
    },
    [isRegisterDisabled, registerDto, register]
  );

  const onEmailSent = useCallback(() => {
    onClose();
    toast({
      title: '验证邮件已发送',
      description: '请跟随邮件内容指示重置密码',
      status: 'success',
      duration: 5000
    });
  }, [onClose, toast]);

  useEffect(() => {
    if (isRegisterSuccess) {
      toast({
        title: '注册成功',
        description: '你的邮箱将会收到一封验证邮件，如未收到请查看垃圾邮件',
        status: 'success',
        duration: 10000
      });
    }
  }, [toast, isRegisterSuccess]);

  useEffect(() => {
    if (isLogInSuccess) {
      window.location.href = redirect ?? "/";
    }
  });

  return (
    <Center minHeight="100vh">
      <Box boxShadow="2xl" bg="gray.700" rounded="lg" px="48px" py="24px">
        <VStack>
          <Center mb="24px">
            <Heading w="120px" h="120px">
              <LogoIcon />
            </Heading>
          </Center>
          {redirect && (
            <Alert status="info" maxW="100%" my="24px">
              <AlertIcon />
              请登录后再访问此网页
            </Alert>
          )}
          <Center>
            <Heading
              size="lg"
              cursor="pointer"
              color={or(!isToLogin, 'gray.500')}
              onClick={toggleIsToLogin}
            >
              登录
            </Heading>
            <Heading
              size="lg"
              pl="12px"
              cursor="pointer"
              color={or(isToLogin, 'gray.500')}
              onClick={toggleIsToLogin}
            >
              注册
            </Heading>
          </Center>
          {!isToLogin && (
            <form onSubmit={onRegister}>
              <FormControl id="username" my="12px" isRequired isInvalid={!!registerError}>
                <FormLabel>用户名</FormLabel>
                <Input
                  autoComplete="no"
                  type="text"
                  value={registerDto.userName}
                  onChange={(event) =>
                    setRegisterDto({ ...registerDto, userName: event.target.value })
                  }
                />
              </FormControl>
              <FormControl id="email" my="12px" isRequired isInvalid={!!registerError}>
                <FormLabel>邮箱</FormLabel>
                <Input
                  type="email"
                  value={registerDto.email}
                  onChange={(event) =>
                    setRegisterDto({ ...registerDto, email: event.target.value })
                  }
                />
              </FormControl>
              <FormControl id="password" my="12px" isRequired isInvalid={!!registerError}>
                <FormLabel>密码</FormLabel>
                <Input
                  autoComplete="no"
                  type="password"
                  value={registerDto.password}
                  onChange={(event) =>
                    setRegisterDto({ ...registerDto, password: event.target.value })
                  }
                />
                {registerError && (
                  <FormErrorMessage>{resolveMessageForRateLimit(registerError)}</FormErrorMessage>
                )}
              </FormControl>
              <Flex mt="24px">
                <Spacer />
                <Button
                  type="submit"
                  variant="solid"
                  isLoading={isRegistering}
                  disabled={isRegistering || isRegisterDisabled || isRegisterSuccess}
                >
                  注册
                </Button>
              </Flex>
            </form>
          )}
          {isToLogin && (
            <form onSubmit={onLogin}>
              <FormControl id="username" my="12px" isInvalid={!!loginError}>
                <FormLabel>用户名或邮箱</FormLabel>
                <Input
                  autoComplete="username"
                  type="text"
                  value={loginDto.userName}
                  onChange={(event) => setLoginDto({ ...loginDto, userName: event.target.value })}
                />
              </FormControl>
              <FormControl id="password" my="12px" isInvalid={!!loginError}>
                <FormLabel>密码</FormLabel>
                <Input
                  autoComplete="current-password"
                  type="password"
                  value={loginDto.password}
                  onChange={(event) => setLoginDto({ ...loginDto, password: event.target.value })}
                />
                {loginError && <FormErrorMessage>{resolveMessageForRateLimit(loginError)}</FormErrorMessage>}
              </FormControl>
              <Flex mt="24px">
                <Button variant="link" onClick={onOpen}>
                  忘记密码
                </Button>
                <Spacer />
                <Button
                  type="submit"
                  variant="solid"
                  disabled={isLoggingIn || isLoginDisabled}
                  isLoading={isLoggingIn}
                >
                  登录
                </Button>
              </Flex>
            </form>
          )}
        </VStack>
      </Box>
      <ForgetPasswordModal isOpen={isOpen} onClose={onClose} onEmailSent={onEmailSent} />
    </Center>
  );
};
