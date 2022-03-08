import React, { FC, FormEvent, useEffect, useState } from 'react';
import {
  Button,
  Flex,
  FormControl,
  FormErrorMessage,
  FormLabel,
  Input,
  Modal,
  ModalBody,
  ModalCloseButton,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalOverlay
} from '@chakra-ui/react';
import { resolveMessage } from '../utils';
import { USER_API } from '../../redux/user.api';

export interface ForgetPasswordModalProps {
  isOpen: boolean;
  onClose: () => any;
  onEmailSent: () => any;
}

export const ForgetPasswordModal: FC<ForgetPasswordModalProps> = ({
  isOpen,
  onClose,
  onEmailSent
}) => {
  const [recover, { isLoading, error, isSuccess }] = USER_API.useRecoveryMutation();
  const [email, setEmail] = useState('');

  const onSubmit = React.useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (email) {
        recover({ email });
      }
    },
    [email, recover]
  );

  useEffect(() => {
    if (isSuccess) {
      onEmailSent();
    }
  }, [isSuccess, onEmailSent]);

  return (
    <Modal isOpen={isOpen} onClose={onClose} closeOnOverlayClick={false} isCentered>
      <ModalOverlay />
      <form onSubmit={onSubmit}>
        <ModalContent>
          <ModalHeader>重置密码</ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <FormControl id="email" my="12px" isInvalid={!!error}>
              <FormLabel>账号邮箱</FormLabel>
              <Input
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />
              {error && <FormErrorMessage>{resolveMessage(error)}</FormErrorMessage>}
            </FormControl>
          </ModalBody>
          <ModalFooter>
            <Flex direction="row-reverse">
              <Button
                type="submit"
                isLoading={isLoading}
                disabled={!email || isLoading || isSuccess}
              >
                提交
              </Button>
            </Flex>
          </ModalFooter>
        </ModalContent>
      </form>
    </Modal>
  );
};
