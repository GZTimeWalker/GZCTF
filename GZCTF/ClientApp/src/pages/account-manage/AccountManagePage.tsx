import React, { FC, FormEvent, useCallback, useEffect, useState } from 'react';
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
import { USER_API, UserChangePasswordDto } from '../../redux/user.api';

export const AccountManagePage: FC = () => {
  const [changeEmail, { isLoading, error, isSuccess }] = USER_API.useChangeEmailMutation();
  const [changePwd, { isLoading: isPwdLoading, error: pwdError, isSuccess: isPwdSuccess }] =
    USER_API.useChangePasswordMutation();
  const [email, setEmail] = useState('');
  const [repeatPwd, setRepeatPwd] = useState('');
  const [repeatPwdError, setRepeatPwdError] = useState(false);
  const [changePasswordDto, setChangePasswordDto] = useState<UserChangePasswordDto>({
    old: '',
    new: ''
  });
  const toast = useToast();

  useEffect(() => {
    if (isPwdSuccess) {
      toast({
        title: '更改成功',
        description: '你的登录密码已更改，请妥善保存',
        status: 'success',
        duration: 10000
      });
    }
  }, [isPwdSuccess, toast]);

  useEffect(() => {
    if (isSuccess) {
      toast({
        title: '提交成功',
        description: '验证邮件已经发往指定的邮箱，请跟随邮箱内容指示操作',
        status: 'success',
        duration: 10000
      });
    }
  }, [isSuccess, toast]);

  const onSubmitEmail = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (email) {
        changeEmail({ newMail: email });
      }
    },
    [email, changeEmail]
  );

  const onChangePwd = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (changePasswordDto.old && changePasswordDto.new && changePasswordDto.new === repeatPwd) {
        changePwd(changePasswordDto);
      }
    },
    [changePasswordDto, repeatPwd, changePwd]
  );

  return (
    <Container p="32px" maxWidth="40ch">
      <Heading mb="32px" size="lg">
        修改邮箱
      </Heading>
      <form onSubmit={onSubmitEmail}>
        <FormControl id="email" my="12px" isRequired isInvalid={!!error}>
          <FormLabel>新邮箱</FormLabel>
          <Input type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
          {error && <FormErrorMessage>{resolveMessage(error)}</FormErrorMessage>}
        </FormControl>
        <Flex mt="32px" direction="row-reverse">
          <Button type="submit" isLoading={isLoading} disabled={!email || isLoading}>
            提交
          </Button>
        </Flex>
      </form>

      <Heading my="32px" size="lg">
        修改密码
      </Heading>
      <form onSubmit={onChangePwd}>
        <FormControl id="oldpassword" my="12px" isRequired isInvalid={!!pwdError}>
          <FormLabel>旧密码</FormLabel>
          <Input
            autoComplete="no"
            type="password"
            value={changePasswordDto.old}
            onChange={(event) =>
              setChangePasswordDto({ ...changePasswordDto, old: event.target.value })
            }
          />
        </FormControl>
        <FormControl id="newpassword" my="12px" isRequired isInvalid={!!pwdError}>
          <FormLabel>新密码</FormLabel>
          <Input
            autoComplete="no"
            type="password"
            value={changePasswordDto.new}
            onChange={(event) =>
              setChangePasswordDto({ ...changePasswordDto, new: event.target.value })
            }
          />
        </FormControl>
        <FormControl
          id="repeatnewpassword"
          my="12px"
          isRequired
          isInvalid={!!pwdError || !!repeatPwdError}
        >
          <FormLabel>重复新密码</FormLabel>
          <Input
            autoComplete="no"
            type="password"
            value={repeatPwd}
            onChange={(event) => {
              setRepeatPwd(event.target.value);
              setRepeatPwdError(
                !changePasswordDto.new || changePasswordDto.new !== event.target.value
              );
            }}
          />
          {pwdError && <FormErrorMessage>{resolveMessage(pwdError)}</FormErrorMessage>}
          {repeatPwdError && <FormErrorMessage>两次输入的密码不相同</FormErrorMessage>}
        </FormControl>
        <Flex mt="32px" direction="row-reverse">
          <Button
            type="submit"
            isLoading={isPwdLoading}
            disabled={
              !changePasswordDto.old || !changePasswordDto.new || !repeatPwd || isPwdLoading
            }
          >
            提交
          </Button>
        </Flex>
      </form>
    </Container>
  );
};
