import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { mutate } from 'swr'
import { Button, Accordion } from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiBackburger, mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ChallengeModel } from '../../../../../Api'
import ChallengeInfoPart from '../../../../../components/admin/ChallengeInfoPart'
import WithGameTab from '../../../../../components/admin/WithGameTab'
import ChallengeContainerPart from '../../../../../components/admin/ChallengeContainerPart'
import ChallengeAttachmentPart from '../../../../../components/admin/ChallengeAttachmentPart'

const GameChallengeEdit: FC = () => {
  const navigate = useNavigate()
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]

  const { data: challenge } = api.edit.useEditGetGameChallenge(numId, numCId, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const [value, setValue] = useState<string | null>("info")
  const [challengeInfo, setChallengeInfo] = useState<ChallengeModel>({ ...challenge })
  const [disabled, setDisabled] = useState(false)

  useEffect(() => {
    if (challenge) {
      setChallengeInfo({ ...challenge })
    }
  }, [challenge])

  const onUpdate = (challenge: ChallengeModel) => {
    if (challenge) {
      setDisabled(true)
      api.edit
        .editUpdateGameChallenge(numId, numCId, challenge)
        .then((data) => {
          showNotification({
            color: 'teal',
            message: '题目已更新，请分别编辑更新每部分信息',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          mutate(data.data)
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
          })
        })
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  return (
    <WithGameTab
      isLoading={!challenge}
      headProps={{ position: 'left' }}
      head={
        <Button
          leftIcon={<Icon path={mdiBackburger} size={1} />}
          onClick={() => navigate(`/admin/games/${id}/challenges`)}
        >
          返回上级
        </Button>
      }
    >
      <Accordion
        chevronPosition="left"
        styles={{
          control: {
            '&, &:hover': {
              backgroundColor: 'transparent',
            },
          }
        }}
        chevronSize={20}
        value={value}
        onChange={setValue}
      >
        <ChallengeInfoPart
          value="info"
          curValue={value}
          challengeInfo={challengeInfo}
          setChallengeInfo={setChallengeInfo}
          disabled={disabled}
          onUpdate={onUpdate}
        />
        <ChallengeAttachmentPart
          value="attachment"
          curValue={value}
          challengeInfo={challengeInfo}
          setChallengeInfo={setChallengeInfo}
          disabled={disabled}
          onUpdate={onUpdate}
        />
        <ChallengeContainerPart
          value="container"
          curValue={value}
          challengeInfo={challengeInfo}
          setChallengeInfo={setChallengeInfo}
          disabled={disabled}
          onUpdate={onUpdate}
        />
      </Accordion>
    </WithGameTab>
  )
}

export default GameChallengeEdit
