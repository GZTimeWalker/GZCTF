import React, { FC, FormEvent, useCallback, useState } from 'react';
import {
  Alert,
  AlertIcon,
  Button,
  Container,
  Flex,
  FormControl,
  FormErrorMessage,
  FormLabel,
  Heading,
  Input,
  Textarea
} from '@chakra-ui/react';
import { resolveMessage } from '../../common/utils';
import { USER_API } from '../../redux/user.api';

export const ChangeInfoPage: FC = () => {
  const { data } = USER_API.useStatusQuery();
  const [update, { isLoading, error, isSuccess }] = USER_API.useUpdateInfoMutation();
  const [studentId, setStudentId] = useState(data!.studentId);
  const [phoneNumber, setPhoneNumber] = useState(data!.phone);
  const [realName, setRealName] = useState(data!.realName);
  const [description, setDescription] = useState(data!.descr);
  const [userName, setUserName] = useState(data!.name);

  const onSubmit = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      update({
        userName: userName || undefined,
        descr: description || undefined,
        studentId: studentId || undefined,
        phoneNumber: phoneNumber || undefined,
        realName: realName || undefined
      });
    },
    [description, studentId, phoneNumber, realName, userName, update]
  );

  return (
    <Container p="32px" maxWidth="40ch">
      <Heading mb="32px" size="lg">
        修改资料
      </Heading>
      <form onSubmit={onSubmit}>
        {data!.isSYSU && (
            <>
            <Alert status="info" maxW="100%" my="24px" fontSize="sm">
              <AlertIcon />
              根据学校政策，中大的同学如果想要参加招新面试，请务必填写「学号」、「真实姓名」和「电话号码」三个栏目。
            </Alert>
          </>
        )}
        <FormControl id="nick-name" my="12px" isInvalid={!!error}>
          <FormLabel>昵称</FormLabel>
          <Input value={userName} onChange={(event) => setUserName(event.target.value)} />
        </FormControl>
        {data!.isSYSU && (
          <>
            <FormControl id="studentId" my="12px" isInvalid={!!error}>
              <FormLabel>学号</FormLabel>
              <Input
                type="number"
                value={studentId}
                onChange={(event) => setStudentId(event.target.value)}
              />
            </FormControl>
            <FormControl id="real-name" my="12px" isInvalid={!!error}>
              <FormLabel>真实姓名</FormLabel>
              <Input value={realName} onChange={(event) => setRealName(event.target.value)} />
            </FormControl>
            <FormControl id="phone-number" my="12px" isInvalid={!!error}>
              <FormLabel>电话号码</FormLabel>
              <Input
                type="tel"
                value={phoneNumber}
                onChange={(event) => setPhoneNumber(event.target.value)}
              />
            </FormControl>
          </>
        )}
        <FormControl id="description" my="12px" isInvalid={!!error}>
          <FormLabel>介绍</FormLabel>
          <Textarea value={description} onChange={(event) => setDescription(event.target.value)} />
          {error && <FormErrorMessage>{resolveMessage(error)}</FormErrorMessage>}
        </FormControl>
        <Flex mt="32px" direction="row-reverse">
          <Button type="submit" isLoading={isLoading} disabled={isLoading}>
            {isSuccess ? '更新成功' : '更新'}
          </Button>
        </Flex>
      </form>
    </Container>
  );
};
