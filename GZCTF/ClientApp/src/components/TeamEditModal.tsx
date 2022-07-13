import { FC } from 'react';
import { Modal, ModalProps, Stack, Text } from '@mantine/core';
import api, {TeamInfoModel} from '../Api';

interface TeamEditModalProps extends ModalProps {
  team: TeamInfoModel | null;
}

const TeamEditModal: FC<TeamEditModalProps> = (props) => {

  const { team, ...modalProps } = props;

  return (
    <Modal {...modalProps}>
      <Stack>
        <Text>{team?.name}</Text>
      </Stack>
    </Modal>
  );
};

export default TeamEditModal;
