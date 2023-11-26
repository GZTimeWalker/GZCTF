import { FC, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Button, Group, Modal, ModalProps, Stack, Text, Textarea } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useEditChallenge } from '@Utils/useEdit'
import api from '@Api'
import { useTranslation } from '@Utils/I18n'

const FlagCreateModal: FC<ModalProps> = (props) => {
  const [disabled, setDisabled] = useState(false)

  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]
  const [flags, setFlags] = useInputState('')

  const { challenge, mutate } = useEditChallenge(numId, numCId)

  const { t } = useTranslation()

  const onCreate = () => {
    if (!flags) {
      return
    }

    const flagList = flags
      .split('\n')
      .filter((x) => x.trim().length > 0)
      .map((x) => ({ flag: x }))

    setDisabled(true)
    api.edit
      .editAddFlags(numId, numCId, flagList)
      .then(() => {
        showNotification({
          color: 'teal',
          message: 'flag 已创建',
          icon: <Icon path={mdiCheck} size={1} />,
        })
        challenge &&
          mutate({
            ...challenge,
            flags: [...(challenge.flags ?? []), ...flagList],
          })
      })
      .catch((err) => {
        showErrorNotification(err, t)
        setDisabled(false)
      })
      .finally(() => {
        setFlags('')
        setDisabled(false)
        props.onClose()
      })
  }

  return (
    <Modal {...props}>
      <Stack>
        <Text>
          创建 flag，多个 flag 按行分割，每行一个。
          <br />
          每道题目可以拥有多个 flag，获取任意 flag 均可得分。
        </Text>
        <Textarea
          value={flags}
          w="100%"
          disabled={disabled}
          autosize
          minRows={8}
          maxRows={8}
          onChange={setFlags}
          sx={(theme) => ({
            '& textarea': {
              fontFamily: theme.fontFamilyMonospace,
            },
          })}
        />
        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled} onClick={onCreate}>
            创建 flag
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default FlagCreateModal
